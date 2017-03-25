/*
Copyright 2017, Marcus McKinnon, E.J. Wilburn, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Palaver.Data;
using Palaver.Models;

namespace Palaver.Controllers
{
    [Authorize]
    [RequireHttps]
    [Route("api/[controller]/[action]")]
    public class FileHandlerController : Controller
    {
        private const string UPLOADS_DIR = "uploads";
        private const string AUTO_UPLOAD_DIR = "auto";
        private string FULL_UPLOADS_BASE;
        private string FULL_UPLOADS_USER_BASE;
        private string RELATIVE_UPLOADS_USER_BASE;

        private readonly IHostingEnvironment _environment;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PalaverDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly int _userId;

        public FileHandlerController(IHostingEnvironment environment, PalaverDbContext context, UserManager<User> userManager, IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            this._environment = environment;
            this._context = context;
            this._userManager = userManager;
            this._mapper = mapper;
            this._httpContextAccessor = httpContextAccessor;
            this._userId = int.Parse(_userManager.GetUserId(_httpContextAccessor.HttpContext.User));

            FULL_UPLOADS_BASE = Path.Combine(_environment.WebRootPath, UPLOADS_DIR);
            if (!Directory.Exists(FULL_UPLOADS_BASE))
                Directory.CreateDirectory(FULL_UPLOADS_BASE);
            
            FULL_UPLOADS_USER_BASE = Path.Combine(FULL_UPLOADS_BASE, _userId.ToString());
            if (!Directory.Exists(FULL_UPLOADS_USER_BASE))
                Directory.CreateDirectory(FULL_UPLOADS_USER_BASE);

            RELATIVE_UPLOADS_USER_BASE = $"{_httpContextAccessor.HttpContext.Request.PathBase}/{UPLOADS_DIR}/{_userId.ToString()}/";
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AutoUpload(IFormCollection formData)
        {
            List<string> savedFiles = new List<string>();
            string relativeAutoUploadPath = RELATIVE_UPLOADS_USER_BASE + "/" + AUTO_UPLOAD_DIR;

            try
            {
                string fullAutoUploadPath = Path.Combine(FULL_UPLOADS_USER_BASE, AUTO_UPLOAD_DIR);

                if (!Directory.Exists(fullAutoUploadPath))
                    Directory.CreateDirectory(fullAutoUploadPath);

                foreach (var file in formData.Files)
                {
                    if (file.Length > 0)
                    {
                        string extension = Path.GetExtension(file.FileName), savePath = null, randomFileName = null;
                        
                        do 
                        {
                            randomFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
                            savePath = Path.Combine(fullAutoUploadPath, randomFileName);
                        } while (System.IO.File.Exists(savePath));

                        using (var fileStream = new FileStream(savePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        savedFiles.Add($"{relativeAutoUploadPath}/{randomFileName}");
                    }
                }
                return new JsonResult(new { message = "Upload successful.",
                    success = true,
                    path = relativeAutoUploadPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = savedFiles 
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = relativeAutoUploadPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = savedFiles
                });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Upload(IFormCollection formData)
        {
            List<string> savedFiles = new List<string>();
            string fixedPath = CleanPath(formData["path"]);

            try
            {
                string absUploadDir = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);

                if (fixedPath.Contains("..") || fixedPath.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = savedFiles
                    });
                }
                if (!Directory.Exists(absUploadDir))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"Directory not found: {fixedPath}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = new List<string>()
                    });
                }

                foreach (var file in formData.Files)
                {
                    if (file.Length > 0)
                    {
                        string absFilePath = Path.Combine(absUploadDir, file.FileName);

                        if (file.FileName.Contains("..") || file.FileName.Contains(":"))
                        {
                            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return new JsonResult(new { message = "Illegal characters in file name.",
                                success = false,
                                path = fixedPath,
                                baseurl = RELATIVE_UPLOADS_USER_BASE,
                                files = savedFiles
                            });
                        }
                        if (System.IO.File.Exists(file.FileName))
                        {
                            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return new JsonResult(new { message = $"File {file.FileName} already exists, please delete it first.",
                                success = false,
                                path = fixedPath,
                                baseurl = RELATIVE_UPLOADS_USER_BASE,
                                files = savedFiles
                            });
                        }
                        
                        using (var fileStream = new FileStream(absFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        savedFiles.Add($"{fixedPath}/{file.FileName}");
                    }
                }
                return new JsonResult(new { message = "Upload successful.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = savedFiles 
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = savedFiles
                });
            }
        }
                    
        [Authorize]
        [HttpPost]
        public IActionResult CreateDir(string name, string path = "")
        {
            List<string> files = new List<string>();
            string absNewDir, fixedPath = CleanPath(path);

            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "The name of the directory to be created cannot be empty.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (name.Contains("..") || name.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in file name.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (fixedPath.Contains("..") || fixedPath.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }

                absNewDir = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath, name);

                if (Directory.Exists(absNewDir))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = $"Directory already exists: {fixedPath}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                
                Directory.CreateDirectory(absNewDir);

                return new JsonResult(new { message = "Success.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = new List<string>{ (string.IsNullOrEmpty(fixedPath) ? "" : fixedPath + "/") + name }
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
        }
                    
        [Authorize]
        [HttpPost]
        public IActionResult Move(string file, string path)
        {
            List<string> files = new List<string>();
            string fixedPath = CleanPath(path);

            try
            {
                if (string.IsNullOrEmpty(file))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "The name of the file/directory to be moved cannot be empty.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (file.Contains("..") || file.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in file name.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (fixedPath.Contains("..") || fixedPath.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }

                string absFilePath = Path.Combine(FULL_UPLOADS_USER_BASE, file);
                string absDestination = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);
                string fileName = Path.GetFileName(absFilePath);
                string absNewLocation = Path.Combine(absDestination, fileName);

                if (!Directory.Exists(absDestination))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"Directory not found: {fixedPath}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                
                if (Directory.Exists(absNewLocation) || System.IO.File.Exists(absNewLocation))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Move aborted, target file/directory already exists.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }

                if (System.IO.File.Exists(absFilePath))
                    System.IO.File.Move(absFilePath, absNewLocation);
                else if (Directory.Exists(absFilePath))
                    Directory.Move(absFilePath, absNewLocation);
                else
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"File or directory not found: {file}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                
                return new JsonResult(new { message = "Success.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = new List<string>{ fixedPath + "/" + fileName }
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult Delete(string target = "", string path = "")
        {
            List<string> files = new List<string>();
            string absPath, fixedPath = CleanPath(path);
            bool haveTarget = !string.IsNullOrWhiteSpace(target), havePath = !string.IsNullOrWhiteSpace(fixedPath);

            try
            {
                if (!haveTarget && !havePath)
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Target path and filename cannot both be empty.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (haveTarget && (target.Contains("..") || target.Contains(":")))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in file name.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                if (havePath && (fixedPath.Contains("..") || fixedPath.Contains(":")))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }

                absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath, target);

                if ((haveTarget && !System.IO.File.Exists(absPath)) || (havePath && !Directory.Exists(absPath)))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"File or directory not found: {(string.IsNullOrWhiteSpace(path) ? "" : fixedPath + "/")}{target}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }

                if (haveTarget)
                    System.IO.File.Delete(absPath);
                else
                {
                    if (absPath.EndsWith("/"))
                        absPath = absPath.Substring(0, absPath.Length - 1);
                    Directory.Delete(absPath);
                }

                return new JsonResult(new { message = "Success.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
        }
                    
        [Authorize]
        [HttpPost]
        public IActionResult ListFiles(string path = "")
        {
            List<string> files = new List<string>();
            string absPath, fixedPath = CleanPath(path);

            try
            {
                if (fixedPath.Contains("..") || fixedPath.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = files
                    });
                }
                absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);

                foreach (string file in Directory.GetFiles(absPath))
                {
                    files.Add(Path.GetFileName(file));
                }

                return new JsonResult(new { message = "Success.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = files
                });
            }
        }
                    
        [Authorize]
        [HttpPost]
        public IActionResult ListDirs(string path = "")
        {
            List<string> dirs = new List<string>();
            string absPath, fixedPath = CleanPath(path);

            try
            {
                if (fixedPath.Contains("..") || fixedPath.Contains(":"))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Illegal characters in path.",
                        success = false,
                        path = path,
                        files = dirs
                    });
                }
                absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);

                foreach (string dir in Directory.GetDirectories(absPath))
                {
                    dirs.Add(Path.GetFileName(dir));
                }

                return new JsonResult(new { message = "Success.",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = dirs
                });
            }   
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = dirs
                });
            }
        }

        private string CleanPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";
            else if (path[0] == '/')
                return path.Substring(1);
            else
                return path.Trim();
        }
    }
}

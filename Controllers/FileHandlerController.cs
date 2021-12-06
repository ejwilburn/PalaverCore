/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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
using Microsoft.Extensions.Logging;
using PalaverCore.Data;
using PalaverCore.Models;

namespace PalaverCore.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class FileHandlerController : Controller
    {
        private const string UPLOADS_DIR = "uploads";
        private const string AUTO_UPLOAD_DIR = "auto";
        private const string SHARED_UPLOAD_DIR = "shared";
        private string FULL_UPLOADS_BASE;
        private string FULL_UPLOADS_USER_BASE;
        private string RELATIVE_UPLOADS_USER_BASE;
        private List<string> ALLOWED_EXTENSIONS = new List<string>{ ".jpg", ".jpeg", ".gif", ".png", ".svg" };
        private readonly IWebHostEnvironment _environment;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PalaverDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly int _userId;

        public FileHandlerController(IWebHostEnvironment environment, PalaverDbContext context, UserManager<User> userManager, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
        {
            this._environment = environment;
            this._context = context;
            this._userManager = userManager;
            this._mapper = mapper;
            this._httpContextAccessor = httpContextAccessor;
            this._logger = loggerFactory.CreateLogger<FileHandlerController>();
            this._userId = int.Parse(_userManager.GetUserId(_httpContextAccessor.HttpContext.User));

            FULL_UPLOADS_BASE = Path.Combine(_environment.WebRootPath, UPLOADS_DIR) + Path.DirectorySeparatorChar;
            if (!Directory.Exists(FULL_UPLOADS_BASE))
                Directory.CreateDirectory(FULL_UPLOADS_BASE);

            FULL_UPLOADS_USER_BASE = Path.Combine(FULL_UPLOADS_BASE, _userId.ToString()) + Path.DirectorySeparatorChar;
            if (!Directory.Exists(FULL_UPLOADS_USER_BASE))
                Directory.CreateDirectory(FULL_UPLOADS_USER_BASE);

            RELATIVE_UPLOADS_USER_BASE = $"{_httpContextAccessor.HttpContext.Request.PathBase}/{UPLOADS_DIR}/{_userId.ToString()}/";
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AutoUpload(IFormCollection formData)
        {
            string relativeAutoUploadPath = RELATIVE_UPLOADS_USER_BASE + AUTO_UPLOAD_DIR,
                savePath = null, randomFileName = null;

            try
            {
                string fullAutoUploadPath = Path.Combine(FULL_UPLOADS_USER_BASE, AUTO_UPLOAD_DIR);
                List<string> savedFiles = new List<string>();

                if (!Directory.Exists(fullAutoUploadPath))
                    Directory.CreateDirectory(fullAutoUploadPath);

                if (formData.Files.Count > 0 && formData.Files[0].Length > 0)
                {
                    var file = formData.Files[0];
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    if (!ALLOWED_EXTENSIONS.Contains(extension))
                    {
                        HttpContext.Response.StatusCode = 500;
                        return new JsonResult(new { uploaded = 0, error = new { message = $"File type not allowed: {extension}" } });
                    }

                    // Generate random file names until one is found that doesn't exist in the target dir.
                    do
                    {
                        randomFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
                        savePath = Path.Combine(fullAutoUploadPath, randomFileName);
                    } while (System.IO.File.Exists(savePath));

                    using (var fileStream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                }

                return new JsonResult(new { uploaded = 1, filename = randomFileName, url = $"{relativeAutoUploadPath}/{randomFileName}" });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return new JsonResult(new { uploaded = 0, error = new { message = ex.Message } });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Upload(IFormCollection formData)
        {
            string path = formData["path"];

            try
            {
                string fixedPath = CleanPath(path);
                string absUploadDir = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);
                List<string> savedFiles = new List<string>();

                if (!Directory.Exists(absUploadDir))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"Directory not found: {path}",
                        success = false,
                        path = path,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = (string[])null
                    });
                }

                foreach (var file in formData.Files)
                {
                    if (file.Length > 0)
                    {
                        string extension = Path.GetExtension(file.FileName);
                        if (!IsAlloweExtension(extension))
                        {
                            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return new JsonResult(new { message = "Invalid file type, allowed extensions: " + String.Join(", ", ALLOWED_EXTENSIONS),
                                success = false,
                                path = path,
                                baseurl = RELATIVE_UPLOADS_USER_BASE,
                                files = (string[])null
                            });
                        }

                        string relativeFilePath = CleanPath(Path.Combine(fixedPath, file.FileName));
                        string absFilePath = Path.Combine(FULL_UPLOADS_USER_BASE, relativeFilePath);
                        if (System.IO.File.Exists(absFilePath))
                        {
                            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return new JsonResult(new { message = $"File {relativeFilePath} already exists, cannot overwrite.",
                                success = false,
                                path = path,
                                baseurl = RELATIVE_UPLOADS_USER_BASE,
                                files = (string[])null
                            });
                        }

                        using (var fileStream = new FileStream(absFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        savedFiles.Add(relativeFilePath);
                    }
                }
                return new JsonResult(new { message = "",
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
                    path = path,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult CreateDir(string name, string path = "")
        {
            try
            {
                string fixedPath = CleanPath(path);

                if (string.IsNullOrEmpty(name))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "The name of the directory to be created cannot be empty.",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = (string[])null
                    });
                }

                string relativeNewDir = CleanPath(Path.Combine(fixedPath, name));
                string absNewDir = Path.Combine(FULL_UPLOADS_USER_BASE, relativeNewDir);
                if (Directory.Exists(absNewDir))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = $"Directory already exists: {fixedPath}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = (string[])null
                    });
                }

                Directory.CreateDirectory(absNewDir);

                return new JsonResult(new { message = "",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = new List<string>{ relativeNewDir }
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = path,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
        }

        // [Authorize]
        // [HttpPost]
        // public IActionResult Move(string file, string path)
        // {
        //     try
        //     {
        //         string fixedPath = CleanPath(path);

        //         if (string.IsNullOrEmpty(file))
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //             return new JsonResult(new { message = "The name of the file/directory to be moved cannot be empty.",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }
        //         if (file.Contains("..") || file.Contains(":"))
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //             return new JsonResult(new { message = "Illegal characters in file name.",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }
        //         if (fixedPath.Contains("..") || fixedPath.Contains(":"))
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //             return new JsonResult(new { message = "Illegal characters in path.",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }

        //         string absFilePath = Path.Combine(FULL_UPLOADS_USER_BASE, file);
        //         string absDestination = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);
        //         string fileName = Path.GetFileName(absFilePath);
        //         string absNewLocation = Path.Combine(absDestination, fileName);

        //         if (!Directory.Exists(absDestination))
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        //             return new JsonResult(new { message = $"Directory not found: {fixedPath}",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }

        //         if (Directory.Exists(absNewLocation) || System.IO.File.Exists(absNewLocation))
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //             return new JsonResult(new { message = "Move aborted, target file/directory already exists.",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }

        //         if (System.IO.File.Exists(absFilePath))
        //             System.IO.File.Move(absFilePath, absNewLocation);
        //         else if (Directory.Exists(absFilePath))
        //             Directory.Move(absFilePath, absNewLocation);
        //         else
        //         {
        //             HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        //             return new JsonResult(new { message = $"File or directory not found: {file}",
        //                 success = false,
        //                 path = path,
        //                 baseurl = RELATIVE_UPLOADS_USER_BASE,
        //                 files = (string[])null
        //             });
        //         }

        //         return new JsonResult(new { message = "",
        //             success = true,
        //             path = fixedPath,
        //             baseurl = RELATIVE_UPLOADS_USER_BASE,
        //             files = new List<string>{ fixedPath + "/" + fileName }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //         return new JsonResult(new { message = ex.Message,
        //             success = false,
        //             path = path,
        //             baseurl = RELATIVE_UPLOADS_USER_BASE,
        //             files = (string[])null
        //         });
        //     }
        // }

        [Authorize]
        [HttpPost]
        public IActionResult Delete(string target = "", string path = "")
        {
            try
            {
                bool haveTarget = !string.IsNullOrWhiteSpace(target), havePath = !string.IsNullOrWhiteSpace(path);
                if (!haveTarget && !havePath)
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return new JsonResult(new { message = "Target path and filename cannot both be empty.",
                        success = false,
                        path = path,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = (string[])null
                    });
                }

                string fixedPath = CleanPath(Path.Combine(path, target));


                string absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath, target);
                if ((haveTarget && !System.IO.File.Exists(absPath)) || (havePath && !Directory.Exists(absPath)))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return new JsonResult(new { message = $"File or directory not found: {(string.IsNullOrWhiteSpace(path) ? "" : fixedPath + "/")}{target}",
                        success = false,
                        path = fixedPath,
                        baseurl = RELATIVE_UPLOADS_USER_BASE,
                        files = (string[])null
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

                return new JsonResult(new { message = "",
                    success = true,
                    path = fixedPath,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return new JsonResult(new { message = ex.Message,
                    success = false,
                    path = path,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult ListFiles(string path = "")
        {
            try
            {
                string fixedPath = CleanPath(path);
                List<string> files = new List<string>();

                string absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);
                foreach (string file in Directory.GetFiles(absPath))
                {
                    files.Add(Path.GetFileName(file));
                }

                return new JsonResult(new { message = "",
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
                    path = path,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult ListDirs(string path = "")
        {
            try
            {
                string fixedPath = CleanPath(path);
                List<string> dirs = new List<string>();

                // If this is a subdir, include ".." so the filemanager can go back up directories.
                if (fixedPath.Length > 0)
                    dirs.Add("..");

                string absPath = Path.Combine(FULL_UPLOADS_USER_BASE, fixedPath);
                foreach (string dir in Directory.GetDirectories(absPath))
                {
                    dirs.Add(Path.GetFileName(dir));
                }

                return new JsonResult(new { message = "",
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
                    path = path,
                    baseurl = RELATIVE_UPLOADS_USER_BASE,
                    files = (string[])null
                });
            }
        }

        private string CleanPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            // Combine the path with the uploads user base and then ensure the resulting path
            // starts with the uploads user base path when resolved with Path.GetFullPath()
            string fullPath = Path.GetFullPath(Path.Combine(FULL_UPLOADS_USER_BASE, path));
            if (!fullPath.StartsWith(FULL_UPLOADS_USER_BASE))
                throw new Exception("Invalid path supplied.");

            return fullPath.Substring(FULL_UPLOADS_USER_BASE.Length);
        }

        private bool IsAlloweExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            return ALLOWED_EXTENSIONS.Contains(extension.ToLower());
        }
    }
}

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

using System.Collections.Generic;
using System.IO;
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
    [Route("/api/[controller]")]
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

            RELATIVE_UPLOADS_USER_BASE = $"{UPLOADS_DIR}/{_userId.ToString()}";
        }

        [Authorize]
        [HttpPost("AutoUpload")]
        public async Task<IActionResult> AutoUpload([FromBody] IFormFileCollection files)
        {
            List<string> savedFiles = new List<string>();
            string fullAutoUploadPath = Path.Combine(FULL_UPLOADS_USER_BASE, AUTO_UPLOAD_DIR),
                relativeAutoUploadPath = RELATIVE_UPLOADS_USER_BASE + "/" + AUTO_UPLOAD_DIR;

            if (!Directory.Exists(fullAutoUploadPath))
                Directory.CreateDirectory(fullAutoUploadPath);

            foreach (var file in files)
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
            return Ok(new { message = "Upload successful.",
                success = true,
                path = relativeAutoUploadPath,
                files = savedFiles 
            });
        }

        [Authorize]
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload([FromBody] string path, [FromBody] IFormFileCollection files)
        {
            List<string> savedFiles = new List<string>();
            string absUploadDir = Url.Content(path);

            if (!Directory.Exists(absUploadDir))
                return Ok(new { message = $"Directory not found: {path}",
                    success = false,
                    path = path,
                    files = new List<string>()
                });

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string absFilePath = Path.Combine(absUploadDir, file.FileName);

                    if (System.IO.File.Exists(file.FileName))
                        return Ok(new { message = $"File {file.FileName} already exists, please delete it first.",
                            success = false,
                            path = path,
                            files = new List<string>()
                        });
                    
                    using (var fileStream = new FileStream(absFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    savedFiles.Add($"{path}/{file.FileName}");
                }
            }
            return Ok(new { message = "Upload successful.",
                success = true,
                path = path,
                files = savedFiles 
            });
        }
                    
        [Authorize]
        [HttpPost("CreateDir")]
        public async Task<IActionResult> CreateDir([FromBody] string dirName, [FromBody] string path)
        {
            string absBasePath = Url.Content(path);
            string absNewDir = Path.Combine(absBasePath, dirName);

            if (!Directory.Exists(absBasePath))
                return Ok(new { message = $"Directory not found: {path}",
                    success = false,
                    path = path,
                    files = new List<string>()
                });
            
            Directory.CreateDirectory(absNewDir);

            return Ok(new { message = "Success.",
                success = true,
                path = path,
                files = new List<string>{ path + "/" + dirName }
            });
        }
                    
        [Authorize]
        [HttpPost("Move")]
        public async Task<IActionResult> Move([FromBody] string file, [FromBody] string path)
        {
            string absFilePath = Url.Content(file);
            string absDestination = Url.Content(path);
            string fileName = Path.GetFileName(absFilePath);

            if (!Directory.Exists(absDestination))
                return Ok(new { message = $"Directory not found: {path}",
                    success = false,
                    path = path,
                    files = new List<string>()
                });
            if (!System.IO.File.Exists(absFilePath))
                return Ok(new { message = $"File not found: {file}",
                    success = false,
                    path = path,
                    files = new List<string>()
                });
            
            System.IO.File.Move(absFilePath, Path.Combine(absDestination, fileName));

            return Ok(new { message = "Success.",
                success = true,
                path = path,
                files = new List<string>{ path + "/" + fileName }
            });
        }
                    
        [Authorize]
        [HttpPost("Delete")]
        public async Task<IActionResult> Delete([FromBody] string path, [FromBody] string target)
        {
            string absPath = Path.Combine(Url.Content(path), target);

            if (System.IO.File.Exists(absPath))
                System.IO.File.Delete(absPath);
            else if (Directory.Exists(absPath))
                Directory.Delete(absPath);
            else
                return Ok(new { message = $"File or directory not found: {path}/{target}",
                    success = false,
                    path = path,
                    files = new List<string>()
                });

            return Ok(new { message = "Success.",
                success = true,
                path = path,
                files = new List<string>()
            });
        }
                    
        [Authorize]
        [HttpPost("ListFiles")]
        public async Task<IActionResult> ListFiles([FromBody] string path)
        {
            List<string> files = new List<string>();
            string absPath = Url.Content(path);

            foreach (string file in Directory.GetFiles(absPath))
            {
                files.Add(Path.GetFileName(file));
            }

            return Ok(new { message = "Success.",
                success = true,
                path = path,
                files = files
            });
        }
                    
        [Authorize]
        [HttpPost("ListDirs")]
        public async Task<IActionResult> ListDirs([FromBody] string path)
        {
            List<string> dirs = new List<string>();
            string absPath = Url.Content(path);

            foreach (string dir in Directory.GetDirectories(absPath))
            {
                dirs.Add(dir);
            }
            
            return Ok(new { message = "Success.",
                success = true,
                path = path,
                files = dirs
            });
        }
    }
}

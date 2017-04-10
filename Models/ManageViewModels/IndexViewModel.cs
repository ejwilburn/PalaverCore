using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Palaver.Models.ManageViewModels
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public bool NotificationEnabled { get; set; }

        public bool BrowserRemembered { get; set; }
    }
}

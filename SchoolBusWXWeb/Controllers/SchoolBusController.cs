﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SchoolBusWXWeb.Business;
using SchoolBusWXWeb.Models.PmsData;
using SchoolBusWXWeb.Models.SchollBusModels;

namespace SchoolBusWXWeb.Controllers
{
    public class SchoolBusController : ControllerEx // OAuthAndJsSdkController 
    {
        private readonly ISchoolBusBusines _schoolBusBusines;
        public SchoolBusController(ISchoolBusBusines schoolBusBusines)
        {
            _schoolBusBusines = schoolBusBusines;
        }
        /// <summary>
        /// https://localhost:5001/schoolbus/Register
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Register(string pkid = "2c9ab45969dc19990169dd5bb9ea08b5")
        {
            if (string.IsNullOrEmpty(pkid)) return View();
            var data = await _schoolBusBusines.GetTwxuserAsync(pkid);
            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel user)
        {
            if (ModelState.IsValid)
            {
#if DEBUG
                user.wxid = "oBcNx1lHzHxIpKm5m64XX99zTMGs";
#else
                user.wxid=UserInfoe.openid;
#endif

            }
            else
            {
                
            }
            return View();
        }
    }
}
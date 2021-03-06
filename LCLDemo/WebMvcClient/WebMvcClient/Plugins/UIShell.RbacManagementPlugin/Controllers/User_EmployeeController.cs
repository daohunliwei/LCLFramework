/******************************************************* 
*  
* 作者：罗敏贵 
* 说明： 企业员工
* 运行环境：.NET 4.5.0 
* 版本号：1.0.0 
*  
* 历史记录： 
*    创建文件 罗敏贵 20154月22日 星期三
*  
*******************************************************/
using LCL.MvcExtensions;
using LCL.Repositories;
using LCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UIShell.RbacPermissionService;

namespace UIShell.RbacManagementPlugin.Controllers
{
    public class User_EmployeeController : RbacController<User_Employee>
    {
        public User_EmployeeController()
        {
            //ddlUser_Employee(Guid.Empty);
            //ddlUser_User(Guid.Empty);

        }
        public void ddlUser_Employee(Guid dtId)
        {
            var repo = RF.FindRepo<CompanyInfo>();
            var list = repo.FindAll();

            List<SelectListItem> selitem = new List<SelectListItem>();
            if (list.Count() > 0)
            {
                var roots = list;
                foreach (var item in roots)
                {
                    selitem.Add(new SelectListItem { Text = item.UnitInfo.Name, Value = item.UnitInfo.ID.ToString() });
                }
            }
            selitem.Insert(0, new SelectListItem { Text = "==企业员工==", Value = "-1" });
            ViewData["ddlUser_Employee"] = selitem;
        }
        public void ddlUser_User(Guid dtId)
        {
            var repo = RF.FindRepo<User>();
            var list = repo.FindAll();

            List<SelectListItem> selitem = new List<SelectListItem>();
            if (list.Count() > 0)
            {
                var roots = list;
                foreach (var item in roots)
                {
                    selitem.Add(new SelectListItem { Text = item.Name, Value = item.ID.ToString() });
                }
            }
            selitem.Insert(0, new SelectListItem { Text = "==企业员工==", Value = "-1" });
            ViewData["ddlUser_Employee"] = selitem;
        }


    }
}


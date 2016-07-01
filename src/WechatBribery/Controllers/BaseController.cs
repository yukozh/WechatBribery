using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WechatBribery.Models;

namespace WechatBribery.Controllers
{
    public class BaseController : BaseController<BriberyContext, IdentityUser, string>
    {
    }
}

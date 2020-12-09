using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer
{
    public interface IRequestManager
    {
        Task OnRequest(HttpContext context);
    }
}
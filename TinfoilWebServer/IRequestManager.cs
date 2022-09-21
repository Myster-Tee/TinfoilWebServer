using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TinfoilWebServer;

public interface IRequestManager
{
    Task OnRequest(HttpContext context);
}
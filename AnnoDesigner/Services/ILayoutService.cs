using System.Threading.Tasks;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Services
{
    public interface ILayoutService
    {
        Task SaveLayoutAsync(IAnnoCanvas canvas, string filePath);
    }
}

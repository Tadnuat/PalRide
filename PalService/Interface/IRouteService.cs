namespace PalService.Interface
{
	using PalService.DTOs;
	using System.Threading.Tasks;

	public interface IRouteService
	{
		Task<ResponseDto<RouteDto>> RegisterRouteAsync(int userId, CreateRouteDto dto);
	}
}


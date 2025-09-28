namespace PalService.Interface
{
	using PalService.DTOs;
	using System.Threading.Tasks;

	public interface IRouteService
	{
		Task<ResponseDto<RouteDto>> RegisterRouteAsync(int userId, CreateRouteDto dto);
		Task<ResponseDto<RouteDto>> UpdateRouteAsync(int userId, int routeId, UpdateRouteDto dto);
		Task<ResponseDto<bool>> DeleteRouteAsync(int userId, int routeId);
		Task<ResponseDto<List<RouteDto>>> GetUserRoutesAsync(int userId);
		Task<ResponseDto<RouteDto>> GetRouteByIdAsync(int routeId);
	}
}








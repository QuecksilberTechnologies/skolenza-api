using AutoMapper;
using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class StaffEmploymentTypeBasicMenuRepository : IStaffEmploymentTypeBasicMenuRepository
    {
        private readonly SkolenzaDbContext? _context;
        private readonly ILogger? _logger;
       
        private readonly IMapper _mapper;
        public StaffEmploymentTypeBasicMenuRepository(SkolenzaDbContext? context, ILogger<StaffEmploymentTypeBasicMenuRepository>? logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            
        }
 

        async Task<IEnumerable<BasicMenuDTO>> IStaffEmploymentTypeBasicMenuRepository.GetBasicMenuDTO(int? staffEmploymentTypeId, int? forPlatform)
        {
            try
            {
                // Log the start of fetching process
                _logger?.LogInformation("Fetching basic menu for StaffEmploymentTypeId: {StaffEmploymentTypeId}, Platform: {ForPlatform}", staffEmploymentTypeId, forPlatform);
                //var menus = await _context.StaffEmploymentTypeBasicsMenus
       //.Where(menu =>
       //    menu.HasAccess == true &&                   // Only menus with access
       //    menu.IsMenuDisplayInUi == true &&           // Only menus set to display in UI
       //    menu.IsActive == true &&                    // Only active menus
       //    menu.ForPlatform == forPlatform &&          // Filter by platform (Web/Mobile)
       //    (staffEmploymentTypeId == null || menu.StaffEmploymentTypeId == staffEmploymentTypeId)) // Filter by StaffEmploymentTypeId if provided
       //   .Include(menu => menu.BasicMenu) // Eager load the BasicMenu entity
       //    .ThenInclude(basicMenu => basicMenu.ParentMenu) // Eager load the ParentMenu (related to BasicMenu)
       //.Select(menu => new BasicMenuDTO
       //{
       //    Id = menu.Id,
       //    MenuName = menu.BasicMenu.MenuName,
       //    MenuUrl = menu.BasicMenu.MenuUrl,
       //    ParentMenuId = menu.BasicMenu.ParentMenuId,
       //    ParentMenuName = menu.BasicMenu.ParentMenu != null ? menu.BasicMenu.ParentMenu.MenuName : null, // Get ParentMenuName
       //    ForPlatform = menu.ForPlatform,
       //    ImageIcon = menu.BasicMenu.ImageIcon,
       //    IsMenuDisplayInUi = menu.IsMenuDisplayInUi,
       //    IsDisplayable = menu.IsDisplayable
       //})
       //.ToListAsync();


               // var tttt = menus;

                // Organize into hierarchical structure (optional based on requirement)


                // Log success
                _logger?.LogInformation("Basic menu fetched successfully for StaffEmploymentTypeId: {StaffEmploymentTypeId}, Platform: {ForPlatform}", staffEmploymentTypeId, forPlatform);

                return null; // menus;
            }
            catch (Exception ex)
            {
                // Log error
                _logger?.LogError(ex, "An error occurred while fetching the basic menu for StaffEmploymentTypeId: {StaffEmploymentTypeId}, Platform: {ForPlatform}", staffEmploymentTypeId, forPlatform);

                // Re-throw with user-friendly message
                throw new Exception("Failed to fetch basic menu. Please try again later.", ex);
            }
        }
    }

}

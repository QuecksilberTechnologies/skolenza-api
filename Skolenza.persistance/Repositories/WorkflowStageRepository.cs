using AutoMapper;
using skolenza.application.Constants;
 
using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Repository for managing Workflow Stages.
    /// Implements IWorkflowStagesRepository with full CRUD operations.
    /// </summary>
    public class WorkflowStageRepository : IWorkflowStagesRepository
    {
        #region Fields
       
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<WorkflowStageRepository> _logger;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public WorkflowStageRepository(
            SkolenzaDbContext context,
            ILogger<WorkflowStageRepository> logger,
            IMapper mapper
          )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
           
        }
        #endregion

        #region CRUD Methods

        /// <summary>
        /// Fetch all active workflow stages.
        /// </summary>
        public async Task<List<GetWorkflowStageResponseDTO>> AllAsync(GetWorkflowStageRequestDTO dto)
        {
            try
            {
              
                var query = _context.WorkflowStages.AsQueryable();

                query = query.Where(s => (s.IsSoftDeleted == null || s.IsSoftDeleted == false) && s.StageOrder == 1);

                if (dto.IsActive != null)
                    query = query.Where(s => s.IsActive == dto.IsActive);

                var stages = await query.ToListAsync();

                return _mapper.Map<List<GetWorkflowStageResponseDTO>>(stages);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;

                if (errorMessage.Contains("Invalid column name"))
                {
                    _logger.LogError(ex, $"⚠️ DB column mismatch detected: {errorMessage}");
                    return new List<GetWorkflowStageResponseDTO>();
                }

                _logger.LogError(ex, "❌ Unexpected error fetching workflow stages.");
                return new List<GetWorkflowStageResponseDTO>();
            }
        }


        /// <summary>
        /// Fetch a workflow stage by its unique Id.
        /// </summary>
        public async Task<GetWorkflowStageResponseDTO?> GetByIdAsync(long id)
        {
            try
            {
              

                var stage = await _context.WorkflowStages
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive && (s.IsSoftDeleted == null || s.IsSoftDeleted == false));

                if (stage == null)
                {
                    _logger.LogWarning("⚠️ WorkflowStage with Id {Id} not found.", id);
                    return null;
                }

                return _mapper.Map<GetWorkflowStageResponseDTO>(stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching workflow stage by Id {Id}.", id);
                return null;
            }
        }

        /// <summary>
        /// Add a new workflow stage.
        /// </summary>
        public async Task<List<GetWorkflowStageResponseDTO>> AddAsync(CreateWorkflowStageRequestDTO dto)
        {
            try
            {
              
                var entity = _mapper.Map<WorkflowStage>(dto);
                entity.IsActive = ConstantValues.IsByDefaultTrue;
                entity.AddedById = dto.StaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;
              

                await _context.WorkflowStages.AddAsync(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ WorkflowStage added successfully with Id {Id}.", entity.Id);

                // Return all active workflow stages after addition
                var stages = await _context.WorkflowStages
                    .Where(s => s.IsActive && (s.IsSoftDeleted == null || s.IsSoftDeleted == false))
                    .ToListAsync();

                return _mapper.Map<List<GetWorkflowStageResponseDTO>>(stages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding new workflow stage.");
                return new List<GetWorkflowStageResponseDTO>();
            }
        }

        /// <summary>
        /// Update an existing WorkflowStage record in the database.
        /// </summary>
        /// <param name="dto">The DTO containing updated data for WorkflowStage.</param>
        /// <returns>Returns true if update succeeds; otherwise false.</returns>
        public async Task<bool> UpdateAsync(UpdateWorkflowStageRequestDTO dto)
        {
            try
            {
                // 🧠 Step 1: Validate input
                if (dto == null || dto.Id <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid WorkflowStage update request received.");
                    return false;
                }

                // 🧠 Step 2: Create a new DbContext instance from factory
               
                // 🧠 Step 3: Fetch the entity by Id (only active and not soft-deleted)
                var entity = await _context.WorkflowStages
                    .FirstOrDefaultAsync(s => s.Id == dto.Id && (s.IsSoftDeleted == null || s.IsSoftDeleted == false));

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ WorkflowStage with Id {Id} not found or inactive/soft-deleted.", dto.Id);
                    return false;
                }

                // 🧠 Step 4: Handle nullable IsActive — preserve old value if dto.IsActive is null
                if (dto.IsActive == null)
                    dto.IsActive = entity.IsActive;

                // 🧠 Step 5: Map updated values
                _mapper.Map(dto, entity);

                // 🧠 Step 6: Update audit fields
                entity.UpdatedById = dto.StaffMemberId;
                entity.UpdatedDateTime = DateTime.UtcNow;

                // 🧠 Step 7: Save changes
                _context.WorkflowStages.Update(entity);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("✅ WorkflowStage with Id {Id} updated successfully.", dto.Id);
                    return true;
                }

                _logger.LogWarning("⚠️ WorkflowStage update for Id {Id} did not modify any record.", dto.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating WorkflowStage with Id {Id}.", dto.Id);
                return false;
            }
        }

        /// <summary>
        /// Soft delete a workflow stage by Id.
        /// </summary>
        public async Task<bool> DeleteAsync(long id, long staffMemberId)
        {
            try
            {
                
                var entity = await _context.WorkflowStages
                    .FirstOrDefaultAsync(s => s.Id == id && (s.IsSoftDeleted == null || s.IsSoftDeleted == false));

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ WorkflowStage with Id {Id} not found for delete.", id);
                    return false;
                }

                entity.IsActive = false;
                entity.IsSoftDeleted = true;
                entity.UpdatedById = staffMemberId;
                entity.UpdatedDateTime = DateTime.UtcNow;

                _context.WorkflowStages.Update(entity);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("🗑️ WorkflowStage with Id {Id} soft deleted successfully.", id);
                    return true;
                }

                _logger.LogWarning("⚠️ No changes made while deleting WorkflowStage with Id {Id}.", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting WorkflowStage with Id {Id}.", id);
                return false;
            }
        }

        #endregion
    }


}

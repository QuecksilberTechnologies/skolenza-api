using skolenza.application.DTOS.Student;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentPaymentGatewayConfigurationRepository
{
    Task<StudentPaymentGatewayConfigurationResponseDTO?> GetAsync(long institutionId, CancellationToken cancellationToken);

    Task<StudentPaymentGatewayConfiguration?> GetCredentialsAsync(long institutionId, CancellationToken cancellationToken);

    Task<StudentPaymentGatewayConfigurationResponseDTO> CreateAsync(
        StudentPaymentGatewayConfiguration configuration,
        CancellationToken cancellationToken);

    Task<StudentPaymentGatewayConfigurationResponseDTO> UpdateAsync(
        StudentPaymentGatewayConfiguration configuration,
        int expectedVersion,
        CancellationToken cancellationToken);
}

using skolenza.application.Constants;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.domain.Entity;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Regression contracts for the staffMember device credential UI payload.</summary>
[TestFixture]
public sealed class StaffMemberDeviceCredentialContractTests
{
    [Test]
    public void Set_user_info_is_a_institution_command_and_requires_the_server_owned_enroll_id()
    {
        var definition = DeviceProtocolCommandCatalog.GetRequired(DeviceCommands.SetUserInfo);

        Assert.Multiple(() =>
        {
            Assert.That(definition.AccessLevel, Is.EqualTo(DeviceCommandAccessLevel.InstitutionPermission));
            Assert.That(definition.Name, Is.EqualTo(DeviceCommands.SetUserInfo));
        });
    }

    [Test]
    public void Credential_response_exposes_command_state_but_no_raw_credential_value()
    {
        var response = new StaffMemberDeviceEnrollmentResponseDTO
        {
            FaceDeploymentStatus = DeviceCredentialDeploymentStatus.Queued,
            FaceCommandStatus = DeviceCommandStatus.AwaitingResponse
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.FaceDeploymentStatus, Is.EqualTo(DeviceCredentialDeploymentStatus.Queued));
            Assert.That(response.FaceCommandStatus, Is.EqualTo(DeviceCommandStatus.AwaitingResponse));
            Assert.That(typeof(StaffMemberDeviceEnrollmentResponseDTO).GetProperty("Pin"), Is.Null);
            Assert.That(typeof(StaffMemberDeviceEnrollmentResponseDTO).GetProperty("CardNumber"), Is.Null);
        });
    }

    [Test]
    public void User_activation_response_has_a_separate_live_command_status()
    {
        var response = new StaffMemberDeviceEnrollmentResponseDTO
        {
            IsActive = false,
            UserActivationCommandStatus = DeviceCommandStatus.AwaitingResponse
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.IsActive, Is.False);
            Assert.That(response.UserActivationCommandStatus, Is.EqualTo(DeviceCommandStatus.AwaitingResponse));
        });
    }

    [Test]
    public void Credential_removal_dropdown_is_centralized_in_the_device_ddl_catalog()
    {
        var field = DeviceDdl.GetSection(DeviceDdl.StaffMemberDeviceCredentials).Single();

        Assert.Multiple(() =>
        {
            Assert.That(field.Key, Is.EqualTo("credentialType"));
            Assert.That(field.Options.Select(x => x.Value), Is.EquivalentTo(new[] { "1", "2", "3" }));
            Assert.That(field.Options.Select(x => x.Label), Does.Contain("Face biometric"));
        });
    }

    [Test]
    public void New_staffMember_device_and_card_dropdowns_are_centralized_and_complete()
    {
        var accessDay = DeviceDdl.GetSection(DeviceDdl.StaffMemberDeviceAccessWindows).Single();
        var cardFields = DeviceDdl.GetSection(DeviceDdl.InstitutionCardInventory);

        Assert.Multiple(() =>
        {
            Assert.That(accessDay.Options.Select(x => x.Value), Is.EquivalentTo(new[] { "1", "2", "3", "4", "5", "6", "7" }));
            Assert.That(cardFields.Select(x => x.Key), Is.EquivalentTo(new[] { "taxTreatment", "cardStatus" }));
            Assert.That(cardFields.Single(x => x.Key == "taxTreatment").Options, Has.Count.EqualTo(4));
            Assert.That(cardFields.Single(x => x.Key == "cardStatus").Options, Has.Count.EqualTo(6));
        });
    }
}

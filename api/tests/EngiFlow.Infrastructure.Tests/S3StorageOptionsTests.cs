using EngiFlow.Infrastructure.Storage;

namespace EngiFlow.Infrastructure.Tests;

public sealed class S3StorageOptionsTests
{
    [Fact]
    public void Validate_AllowsIamRoleCredentialsWhenStaticKeysAreAbsent()
    {
        var options = new S3StorageOptions
        {
            BucketName = "engiflow-attachments",
            Region = "us-east-1"
        };

        options.Validate();
    }

    [Fact]
    public void Validate_RequiresStaticCredentialPairWhenOneKeyIsConfigured()
    {
        var options = new S3StorageOptions
        {
            BucketName = "engiflow-attachments",
            Region = "us-east-1",
            AccessKey = "minioadmin"
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("configured together", exception.Message);
    }
}

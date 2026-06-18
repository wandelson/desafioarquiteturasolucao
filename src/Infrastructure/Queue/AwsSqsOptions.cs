namespace Infrastructure.Queue;

public sealed class AwsSqsOptions
{
    public string Host { get; set; } = "localhost";
    public string AccessKey { get; set; } = "test";
    public string SecretKey { get; set; } = "test";
    public string ServiceURL { get; set; } = "http://localhost:4566";
    public bool UseHttp { get; set; } = true;
    public string AuthenticationRegion { get; set; } = "us-east-1";
}
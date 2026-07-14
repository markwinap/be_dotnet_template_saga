using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace OrderService.Tests.Integration;

public sealed class IntegrationTestEnvironment : IAsyncLifetime
{
    private const string SqlPassword = "YourStrong@Passw0rd";

  private INetwork? _network;

  private MsSqlContainer? _sqlContainer;
  private RedisContainer? _redisContainer;

    private IContainer? _serviceBusContainer;
    private string? _serviceBusConfigFilePath;

    public bool IsDockerAvailable { get; private set; } = true;

    public IntegrationTestEnvironment()
    {
      try
      {
        _network = new NetworkBuilder()
          .WithName($"orderservice-it-{Guid.NewGuid():N}")
          .Build();

        _sqlContainer = new MsSqlBuilder()
          .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
          .WithPassword(SqlPassword)
          .WithNetwork(_network)
          .WithNetworkAliases("mssql")
          .Build();

        _redisContainer = new RedisBuilder()
          .WithImage("redis:7")
          .WithNetwork(_network)
          .Build();
      }
      catch (DockerUnavailableException)
      {
        IsDockerAvailable = false;
      }
    }

    public string SqlConnectionString => _sqlContainer!.GetConnectionString();

    public string RedisConnectionString =>
      $"localhost:{_redisContainer!.GetMappedPublicPort(6379)}";

    public string ServiceBusConnectionString =>
        $"Endpoint=sb://localhost:{_serviceBusContainer!.GetMappedPublicPort(5672)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public async Task InitializeAsync()
    {
      if (!IsDockerAvailable)
      {
        return;
      }

      await _network!.CreateAsync();

      await _sqlContainer!.StartAsync();
      await _redisContainer!.StartAsync();

        _serviceBusConfigFilePath = Path.Combine(
            Path.GetTempPath(),
            $"servicebus-emulator-{Guid.NewGuid():N}.json");

        await File.WriteAllTextAsync(_serviceBusConfigFilePath, GetServiceBusConfigJson());

        _serviceBusContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithNetwork(_network)
            .WithPortBinding(5672, true)
            .WithPortBinding(5300, true)
            .WithEnvironment("SQL_SERVER", "mssql")
            .WithEnvironment("MSSQL_SA_PASSWORD", SqlPassword)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("EMULATOR_HTTP_PORT", "5300")
            .WithBindMount(_serviceBusConfigFilePath, "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .Build();

        await _serviceBusContainer.StartAsync();
          await WaitForServiceBusHealthAsync(_serviceBusContainer);
    }

    public async Task DisposeAsync()
    {
      if (!IsDockerAvailable)
      {
        return;
      }

        if (_serviceBusContainer is not null)
        {
            await _serviceBusContainer.DisposeAsync();
        }

      if (_redisContainer is not null)
      {
        await _redisContainer.DisposeAsync();
      }

      if (_sqlContainer is not null)
      {
        await _sqlContainer.DisposeAsync();
      }

      if (_network is not null)
      {
        await _network.DeleteAsync();
      }

        if (_serviceBusConfigFilePath is not null && File.Exists(_serviceBusConfigFilePath))
        {
            File.Delete(_serviceBusConfigFilePath);
        }
    }

    private static string GetServiceBusConfigJson() =>
        """
        {
          "UserConfig": {
            "Namespaces": [
              {
                "Name": "sbemulatorns",
                "Topics": [
                  {
                    "Name": "integration-topic",
                    "Subscriptions": [
                      {
                        "Name": "integration-subscription",
                        "Properties": {
                          "DeadLetteringOnMessageExpiration": false,
                          "DefaultMessageTimeToLive": "PT1H",
                          "LockDuration": "PT1M",
                          "MaxDeliveryCount": 3,
                          "ForwardDeadLetteredMessagesTo": "",
                          "ForwardTo": "",
                          "RequiresSession": false
                        }
                      }
                    ]
                  }
                ]
              }
            ],
            "Logging": {
              "Type": "File"
            }
          }
        }
        """;

      private static async Task WaitForServiceBusHealthAsync(IContainer container)
      {
        var managementPort = container.GetMappedPublicPort(5300);
        using var httpClient = new HttpClient();

        for (var retry = 0; retry < 60; retry++)
        {
          try
          {
            using var response = await httpClient.GetAsync($"http://localhost:{managementPort}/health");
            if (response.IsSuccessStatusCode)
            {
              return;
            }
          }
          catch
          {
          }

          await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Service Bus Emulator health endpoint did not become ready in time.");
      }
}

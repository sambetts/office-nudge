using Common.Engine.Config;
using Microsoft.Extensions.Configuration;

namespace UnitTests;

public class TestsConfig : AppConfig
{
    public TestsConfig(IConfiguration config) : base(config)
    {
    }

}

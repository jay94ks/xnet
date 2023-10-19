## Xnet library
This is a useful library that helps you write server/client apps with very simple code.

### How to install
1. `.NET CLI`: `dotnet add package xnetlib --version 1.0.0`
2. Package Manager: `NuGet\Install-Package xnetlib -Version 1.0.0`
3. Package Reference: `<PackageReference Include="xnetlib" Version="1.0.0" />`

### Simple Example
The code below is everything.
```
class MyPacketProvider :  Xnet.BasicPacketProvider<MyPacketProvider>
{
    /// <summary>
    /// Test packet.
    /// </summary>
    private class PACKET : Xnet.BasicPacket
    {
        public override Task ExecuteAsync(Xnet Connection)
        {
            Console.WriteLine(Connection.IsServerMode
                ? "SERVER RECEIVED: PACKET!" 
                : "CLIENT RECEIVED: PACKET!");

            return Connection.EmitAsync(this);
        }

        protected override void Decode(BinaryReader Reader)
        {
        }

        protected override void Encode(BinaryWriter Writer)
        {
        }
    }

    /// <inheritdoc/>
    protected override void MapTypes()
    {
        Map<PACKET>("test");
    }
}

// ---

var Options = new Xnet.ServerOptions();

Options.Endpoint = new IPEndPoint(IPAddress.Any, 7800);
Options.NetworkId = Guid.Empty;
Options.PacketProviders.Add(new Program());

var Services = new ServiceCollection();
using var Provider = Services.BuildServiceProvider();

await Xnet.Server(Provider, Options, Token);
```

Clients can be implemented in exactly the same way,
except that they use Xnet.ClientOptions and Xnet.Client methods for options.

### To  go more deeper...
Read `Xnet/Xnet.Options.cs`, `Xnet/Xnet.Server.cs` and `Xnet/Xnet.Client.cs` files.
The comments on the Options class and derived classes included in these files will help you understand.

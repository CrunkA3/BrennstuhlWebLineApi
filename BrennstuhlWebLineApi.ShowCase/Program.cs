using System.Net;
using BrennstuhlWebLineApi;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

IEnumerable<Device> devices = await BrennstuhlWebLineFinder.FindAsync(async m =>
                                        {
                                            Console.WriteLine(m.IpAddress.ToString());

                                            m.AddCredential(new NetworkCredential(config["username"], config["password"]));
                                            Console.WriteLine(await m.GetStateInformationAsync());
                                            Console.WriteLine(await m.GetHeaderInformationAsync());
                                        });
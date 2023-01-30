using System.Net;
using BrennstuhlWebLineApi;


Console.WriteLine("Hello, World!");


await BrennstuhlWebLineFinder.FindAsync(async m =>
                                        {
                                            Console.WriteLine(m.IpAddress.ToString());

                                            m.AddCredential(new NetworkCredential("admin", "admin"));
                                            Console.WriteLine(await m.GetStateInformationAsync());
                                            Console.WriteLine(await m.GetHeaderInformationAsync());
                                        });




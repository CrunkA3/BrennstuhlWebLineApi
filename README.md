# BrennstuhlWebLineApi
read and control a Brennstuhl Premium-Web-Line V3

```csharp
await BrennstuhlWebLineFinder.FindAsync(async m =>
                                        {
                                            m.AddCredential(new NetworkCredential("user", "password"));
                                            Console.WriteLine(await m.GetStateInformationAsync());
                                            Console.WriteLine(await m.GetHeaderInformationAsync());
                                        });
```
```json
{
    "bsState": {
        "verSW": "3.3.6",
        "verFS": 33554434,
        "verHW": 768,
        "verBL": 50397186,
        "verImg": 0,
        "imgArmed": "-",
        "features": 2151665534,
        "sysRAM": 256,
        "sysFlash": 1024,
        "fsSize": 0,
        "fsFree": 0,
        "ip": "192.168.1.xxx",
        "mac": "01:02:03:04:05:06",
        "visitors": 36,
        "reset": "POR",
        "tempCPU": "56",
        "dateTime": "1.2.2023 - 20:00",
        "sync": 452,
        "active": 10068
    }
}

{
    "header": {
        "devId": "BSd830",
        "dateTime": "1.2.2023 - 20:00",
        "userType": 3
    }
}
```

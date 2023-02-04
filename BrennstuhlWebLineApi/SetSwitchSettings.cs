using System.Text.Json.Serialization;

public class SetSwitchSettings
{

    [JsonPropertyName("Name0")]
    public string? ChannelName0 { get; set; }


    [JsonPropertyName("Name1")]
    public string? ChannelName1 { get; set; }


    /// <summary>
    /// Schaltzustand nach Systemstart - Relay 0
    /// 0: Off
    /// 1: On
    /// 2: Last state
    /// </summary>
    [JsonPropertyName("bootSt0")]
    public int? BootState0 { get; set; }



    /// <summary>
    /// Schaltzustand nach Systemstart - Relay 1
    /// 0: Off
    /// 1: On
    /// 2: Last state
    /// </summary>
    [JsonPropertyName("bootSt1")]
    public int? BootState1 { get; set; }




    /// <summary>
    /// Einschaltverzögerung nach Systemstart in Sekunden - Relay 0
    /// </summary>
    [JsonPropertyName("delay0")]
    public int? Delay0 { get; set; }

    /// <summary>
    /// Einschaltverzögerung nach Systemstart in Sekunden - Relay 1
    /// </summary>
    [JsonPropertyName("delay1")]
    public int? Delay1 { get; set; }




    /// <summary>
    /// Automatisches Wiedereinschalten nach Ausschalten nach x Sekunden (0 = deaktiviert) - Relay 0
    /// </summary>
    [JsonPropertyName("reset0")]
    public int? Reset0 { get; set; }


    /// <summary>
    /// Automatisches Wiedereinschalten nach Ausschalten nach x Sekunden (0 = deaktiviert) - Relay 1
    /// </summary>
    [JsonPropertyName("reset0")]
    public int? Reset1 { get; set; }
}
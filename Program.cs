using System.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using phigros_song_info;
using phigros_song_info.UnityEngine;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

var gameInformationArgument = new Argument<FileInfo>(
    name: "GameInformation",
    description: "Path to a binary dump of GameInformation");
var tipsProviderArgument = new Argument<FileInfo>(
    name: "TipsProvider",
    description: "Path to a binary dump of TipsProvider");

var rootCommand = new RootCommand("Phigros SongBase data extractor");
rootCommand.AddArgument(gameInformationArgument);
rootCommand.AddArgument(tipsProviderArgument);
rootCommand.SetHandler(RootCommandHandler, gameInformationArgument, tipsProviderArgument);

await rootCommand.InvokeAsync(args);
return;

void RootCommandHandler(FileInfo gameInformation, FileInfo tipsProvider)
{
    DumpGameInformation(gameInformation);
    DumpTips(tipsProvider);
}

void DumpGameInformation(FileInfo gameInformation)
{
    var pattern = "\x16\x00\x00\x00Glaciaxion.SunsetRay.0\x00\x00\x0A\x00\x00\x00Glaciaxion"u8;
    
    var levelData = File.ReadAllBytes(gameInformation.FullName);
    var tmp = new byte[pattern.Length];
    var mainSongsStart = 0;
    var mainSongsFound = false;
    for (var i = 0; i < levelData.Length - pattern.Length; i++)
    {
        Array.Copy(levelData, i, tmp, 0, pattern.Length);
        
        if (!pattern.SequenceEqual(tmp))
        {
            continue;
        }
        
        mainSongsStart = i;
        mainSongsFound = true;
        break;
    }

    if (!mainSongsFound)
    {
        throw new Exception("Could not find SongBase.mainSongs");
    }

    using var stream = new MemoryStream(levelData);
    stream.Seek(mainSongsStart - 4, SeekOrigin.Current);
    
    using var reader = new BinaryReader(stream);
    var songs = new List<SongsItem>();

    for (var i = 0; i < 4; i++)  // main, extra, sideStory, other
    {
        var len = reader.ReadInt32();
        songs.Capacity += len;
        for (var j = 0; j < len; j++)
        {
            var songsId = reader.ReadAlignedString();
            
            Console.WriteLine(songsId);
            
            var songsKey = reader.ReadAlignedString();
            var songsName = reader.ReadAlignedString();
            var songsTitle = reader.ReadAlignedString();
            
            var difficulties = new float[reader.ReadInt32()];
            for (var k = 0; k < difficulties.Length; k++)
            {
                difficulties[k] = reader.ReadSingle();
            }

            var illustrator = reader.ReadAlignedString();

            var charters = new string[reader.ReadInt32()];
            for (var k = 0; k < charters.Length; k++)
            {
                charters[k] = reader.ReadAlignedString();
            }

            var composer = reader.ReadAlignedString();

            var levels = new string[reader.ReadInt32()];
            for (var k = 0; k < levels.Length; k++)
            {
                levels[k] = reader.ReadAlignedString();
            }

            var previewTime = reader.ReadSingle();
            var previewEndTime = reader.ReadSingle();

            var unlockInfo = new ChartUnlock[reader.ReadInt32()];
            for (var k = 0; k < unlockInfo.Length; k++)
            {
                var unlockType = reader.ReadInt32();
                
                var info = new string[reader.ReadInt32()];
                for (var l = 0; l < info.Length; l++)
                {
                    info[l] = reader.ReadAlignedString();
                }

                unlockInfo[k] = new ChartUnlock { unlockType = unlockType, unlockInfo = info };
            }

            var levelMods = new LevelMods[reader.ReadInt32()];
            for (var k = 0; k < levelMods.Length; k++)
            {
                var mods = new string[reader.ReadInt32()];
                for (var l = 0; l < mods.Length; l++)
                {
                    mods[l] = reader.ReadAlignedString();
                }

                levelMods[k] = new LevelMods { levelMods = mods };
            }
            
            var song = new SongsItem
            {
                songsId = songsId,
                songsKey = songsKey,
                songsName = songsName,
                songsTitle = songsTitle,
                difficulty = difficulties,
                illustrator = illustrator,
                charter = charters,
                composer = composer,
                levels = levels,
                previewTime = previewTime,
                previewEndTime = previewEndTime,
                unlockInfo = unlockInfo,
                levelMods = levelMods
            };
            
            songs.Add(song);
        }
    }
    
    songs.Sort((a, b) => string.Compare(a.songsId, b.songsId, StringComparison.Ordinal));

    using var output = File.Open("docs/songs.json", FileMode.Create, FileAccess.Write);

    using var writer = new StreamWriter(output);
    writer.NewLine = "\n";

    using var jw = new JsonTextWriter(writer);
    jw.Formatting = Formatting.Indented;
    jw.IndentChar = ' ';
    jw.Indentation = 4;
    
    JsonSerializer.CreateDefault().Serialize(jw, songs, null);
}

void DumpTips(FileInfo tipsProvider)
{
    var tipsData = File.ReadAllBytes(tipsProvider.FullName);
    var reader = new BinaryReader(new MemoryStream(tipsData));
    var languageCount = reader.ReadInt32();
    var tipCollections = new List<TipsCollection>()
    {
        Capacity = languageCount
    };

    for (var i = 0; i < languageCount; i++)
    {
        var language = reader.ReadInt32();
        var tipCount = reader.ReadInt32();
        var tips = new List<string>()
        {
            Capacity = tipCount
        };

        for (var k = 0; k < tipCount; k++)
        {
            tips.Add(reader.ReadAlignedString());
        }
        
        tips.Sort((a, b) => string.Compare(a, b, StringComparison.Ordinal));

        tipCollections.Add(
            new TipsCollection
            {
                language = (SystemLanguage)language,
                tips = tips,
            }
        );
    }
    
    tipCollections.Sort((a, b) => ((int)a.language).CompareTo((int)b.language));

    using var output = File.Open("docs/tips.json", FileMode.Create, FileAccess.Write);

    using var writer = new StreamWriter(output);
    writer.NewLine = "\n";

    using var jw = new JsonTextWriter(writer);
    jw.Formatting = Formatting.Indented;
    jw.IndentChar = ' ';
    jw.Indentation = 4;
    
    JsonSerializer.CreateDefault().Serialize(jw, tipCollections, null);
}

internal record SongsItem
{
    public required string songsId;

    public required string songsKey;

    public required string songsName;
    
    [JsonIgnore]
    public required string songsTitle;

    public required float[] difficulty;
    
    public required string illustrator;
    
    public required string[] charter;
    
    public required string composer;
    
    public required string[] levels;
    
    public required float previewTime;
    
    public required float previewEndTime;
    
    public required ChartUnlock[] unlockInfo;
    
    public required LevelMods[] levelMods;
}

internal class ChartUnlock
{
    public required int unlockType;
    
    public required string[] unlockInfo;
}

internal class LevelMods
{
    public required string[] levelMods;
}

internal class TipsCollection
{
    [JsonConverter(typeof(StringEnumConverter))]
    public required SystemLanguage language;

    public required List<string> tips;
}

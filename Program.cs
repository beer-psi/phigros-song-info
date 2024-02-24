using System.CommandLine;
using Newtonsoft.Json;
using phigros_song_info;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

var levelArgument = new Argument<FileInfo>(
    name: "level0",
    description: "Path to level0 file, or whatever file contains SongBase data");
var outputArgument = new Argument<FileInfo>(
    name: "output",
    description: "Output location",
    getDefaultValue: () => new FileInfo("songs.json"));

var rootCommand = new RootCommand("Phigros SongBase data extractor");
rootCommand.AddArgument(levelArgument);
rootCommand.AddArgument(outputArgument);
rootCommand.SetHandler(RootCommandHandler, levelArgument, outputArgument);

await rootCommand.InvokeAsync(args);
return;

void RootCommandHandler(FileInfo levelFile, FileInfo outputFile)
{
    var pattern = "\x16\x00\x00\x00Glaciaxion.SunsetRay.0\x00\x00\x0A\x00\x00\x00Glaciaxion"u8;
    
    var levelData = File.ReadAllBytes(levelFile.FullName);
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

    using var output = File.Open(outputFile.FullName, FileMode.Create, FileAccess.Write);

    using var writer = new StreamWriter(output);
    writer.NewLine = "\n";

    using var jw = new JsonTextWriter(writer);
    jw.Formatting = Formatting.Indented;
    jw.IndentChar = ' ';
    jw.Indentation = 4;
    
    JsonSerializer.CreateDefault().Serialize(jw, songs, null);
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
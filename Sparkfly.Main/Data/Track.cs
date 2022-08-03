﻿namespace Sparkfly.Main.Data;

public class Track
{
    public enum CoverSize { Large, Medium, Small }
    public string? SongId { get; set; }
    public string? SongName { get; set; }
    public string? AlbumName { get; set; }
    public List<string?> CoverSizesUrl { get; set; } = new();
    public List<string?> ArtistsNames { get; set; } = new();

    public Track MakeThisDummy()
    {
        SongId = "0";
        SongName = "Nothing being played";
        AlbumName = "No album";
        ArtistsNames = new() { "No artist" };
        CoverSizesUrl = new() { "", "/img/no_cover.jpg", "" };

        return this;
    }
}

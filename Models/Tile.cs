﻿using Meep.Tech.Data;

namespace Overworld.Data {

  /// <summary>
  /// Represents a tile placed on a tileboard.
  /// </summary>
  public partial struct Tile : IModel<Tile, Tile.Type>, IModel.IUseDefaultUniverse {

    /// <summary>
    /// The archetype originally used to make this tile.
    /// A tile can be modified around it, and then reset to it as well.
    /// </summary>
    public Type Archetype {
      get => _archetype;
      set => _applyArchetype(value);
    } Type _archetype;

    /// <summary>
    /// The background tile this tile is using
    /// </summary>
    public UnityEngine.Tilemaps.Tile Background {
      get => (Archetype?.UseDefaultBackgroundAsInWorldTileImage ?? _backgroundOverride?.UseDefaultBackgroundAsInWorldTileImage ?? false)
        ? _backgroundOverride?.DefaultBackground ?? _background
        : null;
    } UnityEngine.Tilemaps.Tile _background;

    /// <summary>
    /// The tile height
    /// </summary>
    public float Height {
      get;
      set;
    }

    /// <summary>
    /// can be used to reference a type who's background should be used instead.
    /// This is to avoid duplicating tiles.
    /// </summary>
    Type _backgroundOverride;

    Tile(IBuilder<Tile> builder) : this() {
      _applyArchetype((Type)builder.Archetype);
      _background = builder?.GetParam<UnityEngine.Tilemaps.Tile>(nameof(Background)) ?? Background;
      Height = builder?.GetParam<float?>(nameof(Height), null) ?? Height;
    }

    /// <summary>
    /// Resets this tile to it's current archetype's settings, and updates any changed settings.
    /// </summary>
    public void ResetAndUpdateForCurrentArchetype()
      => _applyArchetype(_archetype);

    /// <summary>
    /// Override the background to another type's background image
    /// </summary>
    public void OverrideBackgroundTo(Type archetype) {
      _background = _backgroundOverride.DefaultBackground;
      if(Background is not null) {
        _backgroundOverride = archetype;
      }
    }

    /// <summary>
    /// Initialize this for a new archetype
    /// </summary>
    /// <param name="archetype"></param>
    void _applyArchetype(Type archetype) {
      if(archetype?.LinkArchetypeToTileDataOnSet ?? false) {
        _backgroundOverride = null;
        _archetype = archetype;
        if(archetype.UseDefaultBackgroundAsInWorldTileImage) {
          _background = archetype.DefaultBackground;
        }
      } // if we're not linking, and it has a background, we need to hide it in the override.
      else {
        if(archetype?.DefaultBackground != null) {
          if(archetype.UseDefaultBackgroundAsInWorldTileImage) {
            _backgroundOverride = archetype;
            _background = archetype.DefaultBackground;
          }
        }
      }

      Height = archetype?.DefaultHeight ?? Height;
    }
  }
}

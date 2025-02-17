﻿using Meep.Tech.Data;

namespace Overworld.Data.Entites.Components {

  /// <summary>
  /// This type of hook is exexuted on a key press.
  /// </summary>
  public abstract class EntityKeyPressHook : EntityHook<EntityKeyPressHook> {
    ///<summary><inheritdoc/></summary>
    public override string SimpleUxMenuTitle => "Hook: Key Presses";
  }
}

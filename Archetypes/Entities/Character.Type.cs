﻿using Meep.Tech.Data;
using Meep.Tech.Collections.Generic;
using Overworld.Data.Entities.Components;
using System;
using System.Collections.Generic;
using static Meep.Tech.Data.Configuration.Loader.Settings;

namespace Overworld.Data {

  public partial class Character {

    /// <summary>
    /// The base archetype for player character entities
    /// </summary>
    [Branch]
    public new class Type : Entity.Type {

      ///<summary><inheritdoc/></summary>
      protected override Dictionary<string, object> DefaultTestParams
        => new() {
          {nameof(Character.UniqueName), "Test" }
        };

      ///<summary><inheritdoc/></summary>
      protected override Dictionary<string, Func<IBuilder, IModel.IComponent>> DefaultModelComponentCtors 
        => _DefaultModelComponentCtors ??= base.DefaultModelComponentCtors.AppendOrReplace(Components<BasicPhysicalStats>.Key, builder => Components<BasicPhysicalStats>.BuilderFactory.Make(
          (nameof(BasicPhysicalStats.Height), 2),
          (nameof(BasicPhysicalStats.Weight), 120)
        ));

      Type() 
        : base("Character", "_base.Entities.", "_base.Entities::Character") {}

      /// <summary>
      /// Make a new character with just a name.
      /// </summary>
      public Character Make(string uniqueName)
        => Make<Character>((nameof(Character.UniqueName), uniqueName));
    }
  }
}
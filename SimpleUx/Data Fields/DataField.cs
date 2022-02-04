﻿using Meep.Tech.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using Overworld.Utility;

namespace Overworld.Ux.Simple {

  /// <summary>
  /// A data field for input or display in a simple ux pannel/view
  /// </summary>
  public abstract class DataField : IUxViewElement {

    /// <summary>
    /// The view this field is in.
    /// </summary>
    public View View {
      get;
      internal set;
    }

    /// <summary>
    /// The type of displays available for simple ux data types.
    /// </summary>
    public enum DisplayType {
      Text,
      Toggle,
      RangeSlider,
      Dropdown,
      FieldList,
      KeyValueFieldList,
      Executeable,
      ColorPicker,
      Image,
      Button
    }

    /// <summary>
    /// The type of display this field should use.
    /// </summary>
    public DisplayType Type {
      get;
    }

    /// <summary>
    /// Functions that check if the current field should be enabled.
    /// Called when another field in the same view is updated, or Update is called on the view.
    /// </summary>
    public DelegateCollection<Func<DataField, View, bool>> EnabledIfCheckers {
      get => DefaultEnabledIfCheckers;
      init => value.ForEach(DefaultEnabledIfCheckers.Add);
    }
    /// <summary>
    /// Default enable if checkers added to the field.
    /// </summary>
    protected virtual DelegateCollection<Func<DataField, View, bool>> DefaultEnabledIfCheckers {
      get;
      private set;
    } = new();

    /// <summary>
    /// Functions that check if the current field should be hidden.
    /// Called when another field in the same view is updated, or Update is called on the view.
    /// </summary>
    public DelegateCollection<Func<DataField, View, bool>> HideIfCheckers {
      get => DefaultHideIfCheckers;
      init => value.ForEach(DefaultHideIfCheckers.Add);
    }
    /// <summary>
    /// Default hide if checkers added to the field.
    /// </summary>
    protected virtual DelegateCollection<Func<DataField, View, bool>> DefaultHideIfCheckers {
      get;
      private set;
    } = new();

    /// <summary>
    /// The current value of the field.
    /// </summary>
    public object Value {
      get;
      internal set;
    }

    /// <summary>
    /// The default initial value.
    /// </summary>
    public object DefaultValue {
      get;
      internal set;
    }

    /// <summary>
    /// If this field is readonly
    /// </summary>
    public virtual bool IsReadOnly {
      get;
      internal set;
    } = false;

    /// <summary>
    /// The name of the field.
    /// Used as a default data key
    /// </summary>
    public virtual string Name {
      get;
    }

    /// <summary>
    /// Data key for the field.
    /// Used to access it from the editor component display data.
    /// </summary>
    public virtual string DataKey {
      get;
    }

    /// <summary>
    /// Info tooltip for the field
    /// </summary>
    public virtual string Tooltip {
      get;
    } = null;

    internal DataField _controllerField;

    /// <summary>
    /// Make a new data field for a Simple Ux.
    /// </summary>
    /// <param name="type">the DisplayType to use for this field</param>
    /// <param name="name">the field name. should be unique unless you change the data key</param>
    /// <param name="tooltip">a breif description of the field, will appear on mouse hover in the ui</param>
    /// <param name="value">default/current value of the field</param>
    /// <param name="dataKey">Used to get the value of this field from the view</param>
    /// <param name="isReadOnly">Some read only fields may be formatted differently (like Text). try passing '() => false' to enable if you want a blured out input field instead.</param>
    protected DataField(
      DisplayType type,
      string name,
      string tooltip = null,
      object value = null,
      string dataKey = null,
      bool isReadOnly = false
    ) {
      Type = type;
      Name = name;
      Tooltip = tooltip;
      DefaultValue = Value = value;
      IsReadOnly = isReadOnly;
      DataKey = string.IsNullOrWhiteSpace(dataKey)
        ? name
        : dataKey;

      if(!isReadOnly && DataKey is null) {
        throw new ArgumentException($"Non-read-only fields require a data key. Provide a title, name, or datakey to the field constructor or Make function");
      }
    }

    /// <summary>
    /// Try to update the field value to a new one.
    /// Checks validations and returns an error message if there is one.
    /// </summary>
    public virtual bool TryToSetValue(object value, out string resultMessage) {
      var oldValue = Value;
      resultMessage = "Success!";

      /// for controller fields, that need to be validated by their parent.
      if(_controllerField is not null) {
        (object key, object value)? pair = null;
        if(value is KeyValuePair<string, object> stringKeyedPair) {
          pair = (stringKeyedPair.Key, stringKeyedPair.Value);
        } else if(value is KeyValuePair<int, object> intKeyedPair) {
          pair = (intKeyedPair.Key, intKeyedPair.Value);
        }
        if(pair.HasValue) {
          oldValue = _controllerField.Value;
          if(!((_controllerField as IIndexedItemsDataField)?.TryToUpdateValueAtIndex(pair.Value.key, pair.Value.value, out resultMessage) ?? true)) {
            return false;
          }
        }
      } else if(!RunValidationsOn(value, out resultMessage)) {
        return false;
      }

      Value = value;
      _runOnValueChangedCallbacks(this, oldValue);

      return true;
    }

    /// <summary>
    /// Used to run validations on the given value.
    /// </summary>
    protected abstract bool RunValidationsOn(object value, out string resultMessage);
    internal abstract void _runOnValueChangedCallbacks(DataField updatedField, object oldValue);

    /// <summary>
    /// Memberwise clone to copy
    /// </summary>
    /// <returns></returns>
    public virtual DataField Copy(View toNewView = null, bool withCurrentValuesAsNewDefaults = false) {
      var newField = MemberwiseClone() as DataField;
      newField.View = toNewView;
      newField.DefaultValue = withCurrentValuesAsNewDefaults ? Value : DefaultValue;
      newField.DefaultHideIfCheckers = new(DefaultHideIfCheckers);
      newField.DefaultEnabledIfCheckers = new(DefaultEnabledIfCheckers);

      return newField;
    }

    /// <summary>
    /// Reset the value of this field to it's default
    /// </summary>
    public void ResetValueToDefault()
      => Value = DefaultValue;

    ///<summary><inheritdoc/></summary>
    IUxViewElement IUxViewElement.Copy(View toNewView)
      => Copy(toNewView);

    /// <summary>
    /// Make a new field that fits your needs.
    /// Some field types require attribute data.
    /// </summary>
    public static DataField Make(
      DisplayType type,
      string title = null,
      string tooltip = null,
      object value = null,
      bool isReadOnly = false,
      Func<DataField, View, bool> enabledIf = null,
      string dataKey = null,
      Dictionary<Type, Attribute> attributes = null,
      params Func<DataField, object, (bool success, string message)>[] validations
    ) => Make(type, title, tooltip, value, isReadOnly, enabledIf, dataKey, attributes, validations);

    /// <summary>
    /// Make a new field that fits your needs.
    /// Some field types require attribute data.
    /// </summary>
    public static DataField MakeDefault(
      DisplayType type,
      string title = null,
      string tooltip = null,
      object value = null,
      bool isReadOnly = false,
      string dataKey = null,
      DelegateCollection<Func<DataField, View, bool>> enabledIf = null,
      DelegateCollection<Func<DataField, View, bool>> hiddenIf = null,
      DelegateCollection<Func<DataField, object, (bool success, string message)>> validations = null,
      DelegateCollection<Action<DataField, object>> onValueChanged = null,
      Dictionary<Type, Attribute> attributes = null
    ) {
      switch(type) {
        case DisplayType.Text:
          if(isReadOnly) {
            return new ReadOnlyTextField(
              title: title,
              tooltip: tooltip,
              text: value,
              dataKey: dataKey
            ) {
              HideIfCheckers = hiddenIf
            };
          } else
            return new TextField(
              name: title,
              tooltip: tooltip,
              value: value,
              dataKey: dataKey
            ){
              EnabledIfCheckers = enabledIf,
              HideIfCheckers = hiddenIf,
              Validations = validations.ReDelegate(func => func.CastMiddleType<object, string>()),
              OnValueChangedListeners = onValueChanged.ReDelegate(func => func.CastEndType<object, string>())
            };

        case DisplayType.Toggle:
          bool boolValue = value is bool asBool
            ? asBool
            : float.TryParse(value.ToString(), out float parsedAsFloat) && parsedAsFloat > 0;
          return new ToggleField(
            name: title,
            tooltip: tooltip,
            value: boolValue,
            dataKey: dataKey
          ) {
            EnabledIfCheckers = enabledIf,
            HideIfCheckers = hiddenIf,
            Validations = validations.ReDelegate(func => func.CastMiddleType<object, bool>()),
            OnValueChangedListeners = onValueChanged.ReDelegate(func => func.CastEndType<object, bool>())
          };

        case DisplayType.RangeSlider:
          RangeSliderAttribute rangeSliderAttribute
            = attributes.TryGetValue(typeof(RangeSliderAttribute), out var foundrsa)
              ? foundrsa as RangeSliderAttribute
              : null;

          bool clamped = rangeSliderAttribute?._isClampedToInt ?? false;
          (float min, float max)? minAndMax = rangeSliderAttribute is not null
            ? (rangeSliderAttribute._min, rangeSliderAttribute._max)
            : null;

          float? floatValue = value is double asFloat
            ? (float)asFloat
            : float.TryParse(value.ToString(), out float parsedFloat)
             ? parsedFloat
             : null;

          return new RangeSliderField(
            name: title,
            min: minAndMax?.min ?? 0,
            max: minAndMax?.max ?? 1,
            clampedToWholeNumbers: clamped,
            tooltip: tooltip,
            value: floatValue,
            dataKey: dataKey
          ) {
            EnabledIfCheckers = enabledIf,
            HideIfCheckers = hiddenIf,
            Validations = validations.ReDelegate(func => func.CastMiddleType<object, double>()),
            OnValueChangedListeners = onValueChanged.ReDelegate(func => func.CastEndType<object, double>())
          };

        case DisplayType.KeyValueFieldList:
          return new DataFieldKeyValueSet(
            name: title,
            rows: value as Dictionary<string, object>,
            tooltip: tooltip,
            dataKey: dataKey,
            childFieldAttributes: attributes.Values,
            isReadOnly: isReadOnly
          ) {
            EnabledIfCheckers = enabledIf,
            HideIfCheckers = hiddenIf,
            EntryValidations = validations.ReDelegate(func => func.CastMiddleType<object, KeyValuePair<string, object>>()),
            OnValueChangedListeners = onValueChanged.ReDelegate(func => func.CastEndType<object, OrderedDictionary<string, object>>())
          };

        case DisplayType.Dropdown:
          DropdownAttribute selectableData = attributes.TryGetValue(typeof(DropdownAttribute), out var found)
            ? found as DropdownAttribute
            : null;

          Dictionary<string, object> options = selectableData?._options;
          return new DropdownSelectField(
            name: title,
            options: options ?? throw new ArgumentNullException(nameof(options)),
            tooltip: tooltip,
            maxSelectableValues: selectableData?._selectLimit ?? 1,
            alreadySelectedOptionKeys: value as string[],
            dataKey: dataKey,
            isReadOnly: isReadOnly
          ) {
            EnabledIfCheckers = enabledIf,
            HideIfCheckers = hiddenIf,
            Validations = validations.ReDelegate(func => func.CastMiddleType<object, List<KeyValuePair<string, object>>>()),
            OnValueChangedListeners = onValueChanged.ReDelegate(func => func.CastEndType<object, List<KeyValuePair<string, object>>>())
          };

        case DisplayType.FieldList:
        case DisplayType.Executeable:
        case DisplayType.ColorPicker:
        case DisplayType.Image:
        case DisplayType.Button:
          throw new NotImplementedException(type.ToString());
        default:
          throw new NotSupportedException(type.ToString());
      }
    }
  }

  /// <summary>
  /// A data field for input or display in a simple ux pannel/view
  /// </summary>
  public abstract class DataField<TValue> : DataField {

    /// <summary>
    /// Actions to be executed on change.
    /// Takes the current field, and the old value.
    /// </summary>
    public DelegateCollection<Action<DataField, TValue>> OnValueChangedListeners {
      get => DefaultOnValueChangedListeners;
      init => value.ForEach(DefaultOnValueChangedListeners.Add);
    }
    /// <summary>
    /// Default fields added to the on changed listeners on init.
    /// </summary>
    protected virtual DelegateCollection<Action<DataField, TValue>> DefaultOnValueChangedListeners {
      get;
      private set;
    } = new();

    /// <summary>
    /// Functions that take the current field, and updated object data, and validate it.
    /// Called whenever the value is changed. If the validation fails, the data view's value won't change from it's previous one.
    /// TODO: if a field is invalid, a red X should appear to clear/reset it with a tooltip explaining why it's invalid.
    /// </summary>
    public virtual DelegateCollection<Func<DataField, TValue, (bool success, string message)>> Validations {
      get => DefaultValidations;
      init => value.ForEach(DefaultValidations.Add);
    }
    /// <summary>
    /// Default validations added to the field.
    /// </summary>
    protected virtual DelegateCollection<Func<DataField, TValue, (bool success, string message)>> DefaultValidations {
      get;
      private set;
    } = new();

    /// <summary>
    /// The value(s) selected.
    /// </summary>
    public new TValue Value {
      get => (TValue)base.Value;
      protected set => base.Value = value;
    }

    /// <summary>
    /// For making new datafield types
    /// </summary>
    protected DataField(DisplayType type, string name, string tooltip = null, object value = null, string dataKey = null, bool isReadOnly = false)
      : base(type, name, tooltip, value, dataKey, isReadOnly) { }

    ///<summary><inheritdoc/></summary>
    public override DataField Copy(View toNewView = null, bool withCurrentValuesAsNewDefaults = false) {
      var newField = base.Copy(toNewView, withCurrentValuesAsNewDefaults) as DataField<TValue>;
      newField.DefaultValidations = new(Validations);
      newField.DefaultOnValueChangedListeners = new(DefaultOnValueChangedListeners);

      return newField;
    }

    ///<summary><inheritdoc/></summary>
    protected override bool RunValidationsOn(object value, out string resultMessage) {
      resultMessage = "Value Is Valid! :D";
      TValue convertedValue = (TValue)value;

      if(Validations.Any()) {
        foreach((bool success, string message) in Validations.Select(validator => {
          return validator.Value(this, convertedValue);
        })) {
          if(!success) {
            resultMessage = string.IsNullOrWhiteSpace(message)
              ? "Value did not pass custom validation functions."
              : message;

            return false;
          } else
            resultMessage = message ?? resultMessage;
        }
      }

      return true;
    }

    internal override void _runOnValueChangedCallbacks(DataField updatedField, object oldValue)
      => OnValueChangedListeners
        .ForEach(listener => listener.Value(this, (TValue)oldValue));
  }
}

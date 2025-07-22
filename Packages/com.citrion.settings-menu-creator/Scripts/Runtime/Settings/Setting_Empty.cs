using CitrioN.Common;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(0)]
  public class Setting_Empty : Setting
  {
    public override object GetDefaultValue(SettingsCollection settings) => null;
  }
}
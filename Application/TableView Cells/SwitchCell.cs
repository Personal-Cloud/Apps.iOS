using System;

using UIKit;

namespace NSPersonalCloud.DarwinMobile
{
    public partial class SwitchCell : UITableViewCell
    {
        public SwitchCell(IntPtr handle) : base(handle) { }

        public const string Identifier = "TableViewKeySwitch";

        public event EventHandler<ToggledEventArgs> Clicked;

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            SwitchButton.ValueChanged += (o, e) => {
                Clicked?.Invoke(SwitchButton, new ToggledEventArgs(SwitchButton.On));
            };
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            Reset();
        }

        public bool Enabled
        {
            get => SwitchButton.Enabled;
            set => SwitchButton.Enabled = value;
        }

        public void Update(string title, bool state)
        {
            TitleLabel.Text = title;
            SwitchButton.On = state;
        }

        public void Reset()
        {
            Clicked = null;
        }
    }
}

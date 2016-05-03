using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ui
{
	public class RotaitingPath : UserControl
	{
		static RotaitingPath()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RotaitingPath), new FrameworkPropertyMetadata(typeof(RotaitingPath)));
		}

		public static readonly DependencyProperty GlyphDataProperty = DependencyProperty.Register(
		  nameof(GlyphData),
		  typeof(Geometry),
		  typeof(RotaitingPath)
		);

		public Geometry GlyphData
		{
			get { return (Geometry)GetValue(GlyphDataProperty); }
			set { SetValue(GlyphDataProperty, value); }
		}
	}
}

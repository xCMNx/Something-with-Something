using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Ui.Controls
{
	/// <summary>
	/// Specifies different collapse modes of a GridExpander.
	/// </summary>
	public enum GridExpanderDirection
	{
		/// <summary>
		/// The GridExpander cannot be collapsed or expanded.
		/// </summary>
		None = 0,
		/// <summary>
		/// The column (or row) to the right (or below) the
		/// splitter's column, will be collapsed.
		/// </summary>
		Next = 1,
		/// <summary>
		/// The column (or row) to the left (or above) the
		/// splitter's column, will be collapsed.
		/// </summary>
		Previous = 2
	}

	/// <summary>
	/// An updated version of the standard GridExpander control that includes a centered handle
	/// which allows complete collapsing and expanding of the appropriate grid column or row.
	/// </summary>
	[TemplatePart(Name = GridExpander.ElementHandleName, Type = typeof(ToggleButton))]
	[TemplatePart(Name = GridExpander.ElementTemplateName, Type = typeof(FrameworkElement))]
	[TemplateVisualState(Name = "MouseOver", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Focused", GroupName = "FocusStates")]
	[TemplateVisualState(Name = "Unfocused", GroupName = "FocusStates")]
	//[TemplateVisualState(Name = "Checked", GroupName = "CheckStates")]
	//[TemplateVisualState(Name = "Unchecked", GroupName = "CheckStates")]
	public class GridExpander : System.Windows.Controls.GridSplitter
	{
		private const string ElementHandleName = "ExpanderHandle";
		private const string ElementTemplateName = "TheTemplate";
		private const string ElementGridExpanderBackground = "GridExpanderBackground";

		private ToggleButton _expanderButton;
		private Rectangle _elementGridExpanderBackground;

		private RowDefinition AnimatingRow;
		private ColumnDefinition AnimatingColumn;

		private GridCollapseOrientation _gridCollapseDirection = GridCollapseOrientation.Auto;
		private GridLength _savedGridLength;
		private double _savedActualValue;
		private double _animationTimeMillis = 200;

		#region Direction dependency property
		/// <summary>
		/// Gets or sets a value that indicates the direction in which the row/colum
		/// will be located that is to be expanded and collapsed.
		/// </summary>
		public GridExpanderDirection Direction
		{
			get { return (GridExpanderDirection)GetValue(DirectionProperty); }
			set { SetValue(DirectionProperty, value); }
		}
		/// <summary>
		/// Identifies the Direction dependency property
		/// </summary>
		public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
			nameof(Direction),
			typeof(GridExpanderDirection),
			typeof(GridExpander),
			new PropertyMetadata(GridExpanderDirection.None, new PropertyChangedCallback(OnDirectionPropertyChanged)));
		#endregion

		#region HandleStyle dependency property
		///<summary>
		/// Gets or sets the style that customizes the appearance of the vertical handle
		/// that is used to expand and collapse the GridExpander.
		/// </summary>
		public Style HandleStyle
		{
			get { return (Style)GetValue(HandleStyleProperty); }
			set { SetValue(HandleStyleProperty, value); }
		}
		/// <summary>
		/// Identifies the HandleStyle dependency property
		/// </summary>
		public static readonly DependencyProperty HandleStyleProperty = DependencyProperty.Register(
			nameof(HandleStyle),
			typeof(Style),
			typeof(GridExpander),
			null);
		#endregion

		#region IsAnimated dependency property
		/// <summary>
		/// Gets or sets a value that indicates if the collapse and
		/// expanding actions should be animated.
		/// </summary>
		public bool IsAnimated
		{
			get { return (bool)GetValue(IsAnimatedProperty); }
			set { SetValue(IsAnimatedProperty, value); }
		}
		/// <summary>
		/// Identifies the IsAnimated dependency property
		/// </summary>
		public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
			nameof(IsAnimated),
			typeof(bool),
			typeof(GridExpander),
			null);
		#endregion

		#region IsCollapsed dependency property
		/// <summary>
		/// Gets or sets a value that indicates if the target column is
		/// currently collapsed.
		/// </summary>
		public bool IsCollapsed
		{
			get { return (bool)GetValue(IsCollapsedProperty); }
			set { SetValue(IsCollapsedProperty, value); }
		}

		/// <summary>
		/// Identifies the IsCollapsed dependency property
		/// </summary>
		public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(
			nameof(IsCollapsed),
			typeof(bool),
			typeof(GridExpander),
			new PropertyMetadata(false, new PropertyChangedCallback(OnIsCollapsedPropertyChanged)));
		#endregion

		#region RowHeightAnimation dependency property
		private double RowHeightAnimation
		{
			get { return (double)GetValue(RowHeightAnimationProperty); }
			set { SetValue(RowHeightAnimationProperty, value); }
		}

		private static readonly DependencyProperty RowHeightAnimationProperty = DependencyProperty.Register(
			nameof(RowHeightAnimation),
			typeof(double),
			typeof(GridExpander),
			new PropertyMetadata(new PropertyChangedCallback(RowHeightAnimationChanged)));
		#endregion

		#region ColWidthAnimation dependency property
		private double ColWidthAnimation
		{
			get { return (double)GetValue(ColWidthAnimationProperty); }
			set { SetValue(ColWidthAnimationProperty, value); }
		}

		private static readonly DependencyProperty ColWidthAnimationProperty = DependencyProperty.Register(
			nameof(ColWidthAnimation),
			typeof(double),
			typeof(GridExpander),
			new PropertyMetadata(new PropertyChangedCallback(ColWidthAnimationChanged)));
		#endregion

		// Define Collapsed and Expanded evenets
		public event EventHandler<EventArgs> Collapsed;
		public event EventHandler<EventArgs> Expanded;

		static GridExpander()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(GridExpander),
				new FrameworkPropertyMetadata(typeof(GridExpander)));
		}

		/// <summary>
		/// Initializes a new instance of the GridExpander class,
		/// which inherits from System.Windows.Controls.GridExpander.
		/// </summary>
		public GridExpander()
		{
			// Set default values
			//DefaultStyleKey = typeof(GridExpander);

			VisualStateManager.GoToState(this, "Checked", false);

			//Direction = GridExpanderDirection.None;
			IsAnimated = true;
			this.LayoutUpdated += delegate
			{
				_gridCollapseDirection = GetCollapseDirection();
			};

			// All GridExpander visual states are handled by the parent GridSplitter class.
		}

		/// <summary>
		/// This method is called when the tempalte should be applied to the control.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_expanderButton = GetTemplateChild(ElementHandleName) as ToggleButton;
			_elementGridExpanderBackground = GetTemplateChild(ElementGridExpanderBackground) as Rectangle;

			// Set default direction since we don't have all the components layed out yet.
			_gridCollapseDirection = GetCollapseDirection();

			// Directely call these events so design-time view updates appropriately
			OnDirectionChanged(Direction);
			// Wire up the Checked and Unchecked events of the VerticalGridExpanderHandle.
			if (_expanderButton != null)
			{
				_expanderButton.Checked += new RoutedEventHandler(GridExpanderButton_Checked);
				_expanderButton.Unchecked += new RoutedEventHandler(GridExpanderButton_Unchecked);
				_expanderButton.IsChecked = IsCollapsed;
			}
			OnIsCollapsedChanged(IsCollapsed);

			changeDefinitionTracking();
		}

		private void DefinitionValueChanged(object sender, EventArgs e)
		{
			ChangeState(IsCollapsed);
		}

		DependencyPropertyDescriptor _descriptor = null;
		DefinitionBase _previousDefinitionBinding = null;

		void removeDefinitionBinding()
		{
			if (_previousDefinitionBinding != null && _descriptor != null)
			{
				_descriptor.RemoveValueChanged(_previousDefinitionBinding, DefinitionValueChanged);
				_previousDefinitionBinding = null;
				_descriptor = null;
			}
		}

		void changeDefinitionTracking()
		{
			var def = GetDefinition();
			if (def == null)
				removeDefinitionBinding();
			else
			{
				var depProp = _gridCollapseDirection == GridCollapseOrientation.Rows ? RowDefinition.HeightProperty : ColumnDefinition.WidthProperty;
				if (def == _previousDefinitionBinding && _descriptor != null && _descriptor.DependencyProperty == depProp)
					return;
				removeDefinitionBinding();
				_previousDefinitionBinding = def;
				_descriptor = DependencyPropertyDescriptor.FromProperty(depProp, typeof(ItemsControl));
				_descriptor.AddValueChanged(_previousDefinitionBinding, DefinitionValueChanged);
			}
		}

		/// <summary>
		/// Handles the property change event of the IsCollapsed property.
		/// </summary>
		/// <param name="isCollapsed">The new value for the IsCollapsed property.</param>
		protected virtual void OnIsCollapsedChanged(bool isCollapsed)
		{
			if (_expanderButton != null)
				_expanderButton.IsChecked = isCollapsed;
			ChangeState(isCollapsed);
		}

		/// <summary>
		/// Handles the property change event of the Direction property.
		/// </summary>
		/// <param name="direction">The new value for the Direction property.</param>
		protected virtual void OnDirectionChanged(GridExpanderDirection direction)
		{
			if (_expanderButton == null)
			{
				// There is no expander button so don't attempt to modify it
				return;
			}

			// TODO: Use triggers for setting visibility conditionally instead of doing it here
			if (direction == GridExpanderDirection.None)
			{
				// Hide the handles if the Direction is set to None.
				_expanderButton.Visibility = Visibility.Collapsed;
			}
			else
			{
				// Ensure the handle is Visible.
				_expanderButton.Visibility = Visibility.Visible;
			}
			changeDefinitionTracking();
		}

		/// <summary>
		/// Raises the Collapsed event.
		/// </summary>
		/// <param name="e">Contains event arguments.</param>
		protected virtual void OnCollapsed(EventArgs e)
		{
			Collapsed?.Invoke(this, e);
		}

		/// <summary>
		/// Raises the Expanded event.
		/// </summary>
		/// <param name="e">Contains event arguments.</param>
		protected virtual void OnExpanded(EventArgs e)
		{
			Expanded?.Invoke(this, e);
		}

		DefinitionBase GetDefinition()
		{
			Grid parentGrid = base.Parent as Grid;
			if (parentGrid != null || _gridCollapseDirection != GridCollapseOrientation.Auto)
			{

				DefinitionBase definition = null;
				var byRow = _gridCollapseDirection == GridCollapseOrientation.Rows;
				var definitions = byRow ? (IList)parentGrid.RowDefinitions : parentGrid.ColumnDefinitions;

				// Get the index of the row/col containing the splitter
				var splitterIndex = (int)GetValue(byRow ? Grid.RowProperty : Grid.ColumnProperty);
				if (Direction == GridExpanderDirection.Next)
				{
					if (splitterIndex + 1 < definitions.Count)
						return (DefinitionBase)definitions[splitterIndex + 1];
				}
				else if (splitterIndex - 1 >= 0)
					return definition = (DefinitionBase)definitions[splitterIndex - 1];
			}
			return null;
		}

		bool _isChanging = false;

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			_isChanging = true;
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			_isChanging = false;
		}

		double ActualValue(DefinitionBase definition)
		{
			switch (_gridCollapseDirection)
			{
				case GridCollapseOrientation.Rows: return (definition as RowDefinition).ActualHeight;
				case GridCollapseOrientation.Columns: return (definition as ColumnDefinition).ActualWidth;
			}

			return double.NaN;
		}

		private void ChangeState(bool collapse)
		{
			if (_isAnimating || _isChanging) return;
			DefinitionBase definition = GetDefinition();

			if (definition == null)
				return;

			var byRow = _gridCollapseDirection == GridCollapseOrientation.Rows;
			var actionProp = byRow ? RowDefinition.HeightProperty : ColumnDefinition.WidthProperty;
			double curVal = ActualValue(definition);
			if (collapse)
			{
				if (curVal == 0)
					return;
				_savedGridLength = byRow ? (definition as RowDefinition).Height : (definition as ColumnDefinition).Width;
				_savedActualValue = curVal;
				// Collapse
				if (IsAnimated)
					AnimateCollapse(definition);
				else
					definition.SetValue(actionProp, new GridLength(0));
				// Raise the Collapsed event.
				OnCollapsed(EventArgs.Empty);
			}
			else
			{
				if (_savedActualValue == 0 || curVal == _savedActualValue)
					return;
				// Expand
				if (IsAnimated)
					AnimateExpand(definition);
				else
					definition.SetValue(actionProp, _savedGridLength);
				// Raise the Expanded event.
				OnExpanded(EventArgs.Empty);
			}

			// Activate the background so the splitter can be dragged again.
			_elementGridExpanderBackground.IsHitTestVisible = !collapse;
		}

		/// <summary>
		/// Determine the collapse direction based on the horizontal and vertical alignments
		/// </summary>
		private GridCollapseOrientation GetCollapseDirection()
		{
			if (base.HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				return GridCollapseOrientation.Columns;
			}

			if ((base.VerticalAlignment == VerticalAlignment.Stretch) && (base.ActualWidth <= base.ActualHeight))
			{
				return GridCollapseOrientation.Columns;
			}

			return GridCollapseOrientation.Rows;
		}

		/// <summary>
		/// Handles the Checked event of either the Vertical or Horizontal
		/// GridExpanderHandle ToggleButton.
		/// </summary>
		/// <param name="sender">An instance of the ToggleButton that fired the event.</param>
		/// <param name="e">Contains event arguments for the routed event that fired.</param>
		private void GridExpanderButton_Checked(object sender, RoutedEventArgs e)
		{
			ChangeState(true);
		}

		/// <summary>
		/// Handles the Unchecked event of either the Vertical or Horizontal
		/// GridExpanderHandle ToggleButton.
		/// </summary>
		/// <param name="sender">An instance of the ToggleButton that fired the event.</param>
		/// <param name="e">Contains event arguments for the routed event that fired.</param>
		private void GridExpanderButton_Unchecked(object sender, RoutedEventArgs e)
		{
			ChangeState(false);
		}

		/// <summary>
		/// The IsCollapsed property porperty changed handler.
		/// </summary>
		/// <param name="d">GridExpander that changed IsCollapsed.</param>
		/// <param name="e">An instance of DependencyPropertyChangedEventArgs.</param>
		private static void OnIsCollapsedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GridExpander s = d as GridExpander;

			bool value = (bool)e.NewValue;
			s.OnIsCollapsedChanged(value);
		}

		/// <summary>
		/// The DirectionProperty property changed handler.
		/// </summary>
		/// <param name="d">GridExpander that changed IsCollapsed.</param>
		/// <param name="e">An instance of DependencyPropertyChangedEventArgs.</param>
		private static void OnDirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GridExpander s = d as GridExpander;

			GridExpanderDirection value = (GridExpanderDirection)e.NewValue;
			s.OnDirectionChanged(value);
		}

		private static void RowHeightAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var extender = d as GridExpander;
			extender._isAnimating = true;
			extender.AnimatingRow.Height = new GridLength((double)e.NewValue);
			extender._isAnimating = false;
		}

		private static void ColWidthAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var extender = d as GridExpander;
			extender._isAnimating = true;
			extender.AnimatingColumn.Width = new GridLength((double)e.NewValue);
			extender._isAnimating = false;
		}

		bool _isAnimating = false;

		/// <summary>
		/// Uses DoubleAnimation and a StoryBoard to animated the collapsing
		/// of the specificed ColumnDefinition or RowDefinition.
		/// </summary>
		/// <param name="definition">The RowDefinition or ColumnDefintition that will be collapsed.</param>
		private void AnimateCollapse(object definition)
		{
			double currentValue;

			// Setup the animation and StoryBoard
			DoubleAnimation gridLengthAnimation = new DoubleAnimation() { Duration = new Duration(TimeSpan.FromMilliseconds(_animationTimeMillis)) };
			Storyboard sb = new Storyboard();

			// Add the animation to the StoryBoard
			sb.Children.Add(gridLengthAnimation);

			if (_gridCollapseDirection == GridCollapseOrientation.Rows)
			{
				// Specify the target RowDefinition and property (Height) that will be altered by the animation.
				this.AnimatingRow = (RowDefinition)definition;
				Storyboard.SetTarget(gridLengthAnimation, this);
				Storyboard.SetTargetProperty(gridLengthAnimation, new PropertyPath("RowHeightAnimation"));

				currentValue = AnimatingRow.ActualHeight;
			}
			else
			{
				// Specify the target ColumnDefinition and property (Width) that will be altered by the animation.
				this.AnimatingColumn = (ColumnDefinition)definition;
				Storyboard.SetTarget(gridLengthAnimation, this);
				Storyboard.SetTargetProperty(gridLengthAnimation, new PropertyPath("ColWidthAnimation"));

				currentValue = AnimatingColumn.ActualWidth;
			}

			if (currentValue == 0) return;

			gridLengthAnimation.From = currentValue;
			gridLengthAnimation.To = 0;

			// Start the StoryBoard.
			sb.Begin();
		}

		/// <summary>
		/// Uses DoubleAnimation and a StoryBoard to animate the expansion
		/// of the specificed ColumnDefinition or RowDefinition.
		/// </summary>
		/// <param name="definition">The RowDefinition or ColumnDefintition that will be expanded.</param>
		private void AnimateExpand(object definition)
		{
			double currentValue;
			// Setup the animation and StoryBoard
			DoubleAnimation gridLengthAnimation = new DoubleAnimation() { Duration = new Duration(TimeSpan.FromMilliseconds(_animationTimeMillis)) };
			Storyboard sb = new Storyboard();

			// Add the animation to the StoryBoard
			sb.Children.Add(gridLengthAnimation);

			if (_gridCollapseDirection == GridCollapseOrientation.Rows)
			{
				// Specify the target RowDefinition and property (Height) that will be altered by the animation.
				this.AnimatingRow = (RowDefinition)definition;
				Storyboard.SetTarget(gridLengthAnimation, this);
				Storyboard.SetTargetProperty(gridLengthAnimation, new PropertyPath("RowHeightAnimation"));

				currentValue = AnimatingRow.ActualHeight;
			}
			else
			{
				// Specify the target ColumnDefinition and property (Width) that will be altered by the animation.
				this.AnimatingColumn = (ColumnDefinition)definition;
				Storyboard.SetTarget(gridLengthAnimation, this);
				Storyboard.SetTargetProperty(gridLengthAnimation, new PropertyPath("ColWidthAnimation"));

				currentValue = AnimatingColumn.ActualWidth;
			}

			if (currentValue == _savedActualValue) return;

			gridLengthAnimation.From = currentValue;
			gridLengthAnimation.To = _savedActualValue;

			// Start the StoryBoard.
			sb.Begin();
		}

		/// <summary>
		/// An enumeration that specifies the direction the GridExpander will
		/// be collapased (Rows or Columns).
		/// </summary>
		private enum GridCollapseOrientation
		{
			Auto,
			Columns,
			Rows
		}
	}
}

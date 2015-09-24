using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SIL.Collections
{
	public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
	{
		/// <summary>
		/// Occurs after a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Provides access to the PropertyChanged event handler to derived classes.
		/// </summary>
		protected PropertyChangedEventHandler PropertyChangedHandler
		{
			get
			{
				return PropertyChanged;
			}
		}

		/// <summary>
		/// Occurs before a property value changes.
		/// </summary>
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Provides access to the PropertyChanging event handler to derived classes.
		/// </summary>
		protected PropertyChangingEventHandler PropertyChangingHandler
		{
			get
			{
				return PropertyChanging;
			}
		}

		/// <summary>
		/// Verifies that a property name exists in this ViewModel. This method
		/// can be called before the property is used, for instance before
		/// calling RaisePropertyChanged. It avoids errors when a property name
		/// is changed but some places are missed.
		/// <para>This method is only active in DEBUG mode.</para>
		/// </summary>
		/// <param name="propertyName"></param>
		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		public void VerifyPropertyName(string propertyName)
		{
			var myType = GetType();

			if (!string.IsNullOrEmpty(propertyName) && myType.GetProperty(propertyName) == null)
			{
				var descriptor = this as ICustomTypeDescriptor;

				if (descriptor != null)
				{
					if (descriptor.GetProperties().Cast<PropertyDescriptor>().Any(property => property.Name == propertyName))
						return;
				}

				throw new ArgumentException("Property not found", propertyName);
			}
		}

		/// <summary>
		/// Raises the PropertyChanging event if needed.
		/// </summary>
		/// <remarks>If the propertyName parameter
		/// does not correspond to an existing property on the current class, an
		/// exception is thrown in DEBUG configuration only.</remarks>
		/// <param name="propertyName">The name of the property that
		/// changed.</param>
		[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
			Justification = "This cannot be an event")]
		protected virtual void RaisePropertyChanging(string propertyName)
		{
			VerifyPropertyName(propertyName);

			PropertyChangingEventHandler handler = PropertyChanging;
			if (handler != null)
			{
				handler(this, new PropertyChangingEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Raises the PropertyChanged event if needed.
		/// </summary>
		/// <remarks>If the propertyName parameter
		/// does not correspond to an existing property on the current class, an
		/// exception is thrown in DEBUG configuration only.</remarks>
		/// <param name="propertyName">The name of the property that
		/// changed.</param>
		[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
			Justification = "This cannot be an event")]
		protected virtual void RaisePropertyChanged(string propertyName)
		{
			VerifyPropertyName(propertyName);

			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Raises the PropertyChanging event if needed.
		/// </summary>
		/// <typeparam name="T">The type of the property that
		/// changes.</typeparam>
		/// <param name="propertyExpression">An expression identifying the property
		/// that changes.</param>
		[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
			Justification = "This cannot be an event")]
		[SuppressMessage(
			"Microsoft.Design",
			"CA1006:GenericMethodsShouldProvideTypeParameter",
			Justification = "This syntax is more convenient than other alternatives.")]
		protected virtual void RaisePropertyChanging<T>(Expression<Func<T>> propertyExpression)
		{
			PropertyChangingEventHandler handler = PropertyChanging;
			if (handler != null)
			{
				string propertyName = GetPropertyName(propertyExpression);
				handler(this, new PropertyChangingEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Raises the PropertyChanged event if needed.
		/// </summary>
		/// <typeparam name="T">The type of the property that
		/// changed.</typeparam>
		/// <param name="propertyExpression">An expression identifying the property
		/// that changed.</param>
		[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
			Justification = "This cannot be an event")]
		[SuppressMessage(
			"Microsoft.Design",
			"CA1006:GenericMethodsShouldProvideTypeParameter",
			Justification = "This syntax is more convenient than other alternatives.")]
		protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				string propertyName = GetPropertyName(propertyExpression);
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Extracts the name of a property from an expression.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="propertyExpression">An expression returning the property's name.</param>
		/// <returns>The name of the property returned by the expression.</returns>
		/// <exception cref="ArgumentNullException">If the expression is null.</exception>
		/// <exception cref="ArgumentException">If the expression does not represent a property.</exception>
		protected string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
		{
			if (propertyExpression == null)
			{
				throw new ArgumentNullException("propertyExpression");
			}

			var body = propertyExpression.Body as MemberExpression;

			if (body == null)
			{
				throw new ArgumentException("Invalid argument", "propertyExpression");
			}

			var property = body.Member as PropertyInfo;

			if (property == null)
			{
				throw new ArgumentException("Argument is not a property", "propertyExpression");
			}

			return property.Name;
		}

		/// <summary>
		/// Assigns a new value to the property. Then, raises the
		/// PropertyChanged event if needed. 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		/// changed.</typeparam>
		/// <param name="propertyExpression">An expression identifying the property
		/// that changed.</param>
		/// <param name="field">The field storing the property's value.</param>
		/// <param name="newValue">The property's value after the change
		/// occurred.</param>
		/// <returns>True if the PropertyChanged event has been raised,
		/// false otherwise. The event is not raised if the old
		/// value is equal to the new value.</returns>
		protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue)
		{
			if (EqualityComparer<T>.Default.Equals(field, newValue))
			{
				return false;
			}

			RaisePropertyChanging(propertyExpression);
			field = newValue;
			RaisePropertyChanged(propertyExpression);
			return true;
		}

		/// <summary>
		/// Assigns a new value to the property. Then, raises the
		/// PropertyChanged event if needed. 
		/// </summary>
		/// <typeparam name="T">The type of the property that
		/// changed.</typeparam>
		/// <param name="propertyName">The name of the property that
		/// changed.</param>
		/// <param name="field">The field storing the property's value.</param>
		/// <param name="newValue">The property's value after the change
		/// occurred.</param>
		/// <returns>True if the PropertyChanged event has been raised,
		/// false otherwise. The event is not raised if the old
		/// value is equal to the new value.</returns>
		protected bool Set<T>(string propertyName, ref T field, T newValue)
		{
			if (EqualityComparer<T>.Default.Equals(field, newValue))
			{
				return false;
			}

			RaisePropertyChanging(propertyName);
			field = newValue;
			RaisePropertyChanged(propertyName);
			return true;
		}
	}
}

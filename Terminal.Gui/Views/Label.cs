﻿//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// Aligns the text to the left of the frame.
		/// </summary>
		Left, 
		/// <summary>
		/// Aligns the text to the right side of the frame.
		/// </summary>
		Right, 
		/// <summary>
		/// Centers the text in the frame.
		/// </summary>
		Centered, 
		/// <summary>
		/// Shows the text as justified text in the frame.
		/// </summary>
		Justified
	}

	/// <summary>
	/// The Label <see cref="View"/> displays a string at a given position and supports multiple lines separted by newline characters.
	/// </summary>
	public class Label : View {
		List<ustring> lines = new List<ustring> ();
		bool recalcPending = true;
		ustring text;
		TextAlignment textAlignment;

		static Rect CalcRect (int x, int y, ustring s)
		{
			int mw = 0;
			int ml = 1;

			int cols = 0;
			foreach (var rune in s) {
				if (rune == '\n') {
					ml++;
					if (cols > mw)
						mw = cols;
					cols = 0;
				} else
					cols++;
			}
			if (cols > mw)
				mw = cols;

			return new Rect (x, y, mw, ml);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Label"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="Label"/> will be created at the given
		///   coordinates with the given string. The size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wraping is provided.
		/// </para>
		/// </remarks>
		/// <param name="x">column to locate the Label.</param>
		/// <param name="y">row to locate the Label.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public Label (int x, int y, ustring text) : this (CalcRect (x, y, text), text)
		{
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Label"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="Label"/> will be created at the given
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wraping is provided.
		/// </para>
		/// </remarks>
		/// <param name="rect">Location.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public Label (Rect rect, ustring text) : base (rect)
		{
			this.text = text;
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Label"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="Label"/> will be created using <see cref="LayoutStyle.Computed"/>
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wraping is provided.
		/// </para>
		/// </remarks>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public Label (ustring text) : base ()
		{
			this.text = text;
			var r = CalcRect (0, 0, text);
			Width = r.Width;
			Height = r.Height;
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Label"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="Label"/> will be created using <see cref="LayoutStyle.Computed"/>
		///   coordinates. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wraping is provided.
		/// </para>
		/// </remarks>
		public Label () : this (text: string.Empty) { }

		static char [] whitespace = new char [] { ' ', '\t' };

		static ustring ClipAndJustify (ustring str, int width, TextAlignment talign)
		{
			// Get rid of any '\r' added by Windows
			str = str.Replace ("\r", ustring.Empty);
			int slen = str.RuneCount;
			if (slen > width){
				var uints = str.ToRunes (width);
				var runes = new Rune [uints.Length];
				for (int i = 0; i < uints.Length; i++)
					runes [i] = uints [i];
				return ustring.Make (runes);
			} else {
				if (talign == TextAlignment.Justified) {
					// TODO: ustring needs this
			               	var words = str.ToString ().Split (whitespace, StringSplitOptions.RemoveEmptyEntries);
					int textCount = words.Sum (arg => arg.Length);

					var spaces = (width- textCount) / (words.Length - 1);
					var extras = (width - textCount) % words.Length;

					var s = new System.Text.StringBuilder ();
					//s.Append ($"tc={textCount} sp={spaces},x={extras} - ");
					for (int w = 0; w < words.Length; w++) {
						var x = words [w];
						s.Append (x);
						if (w + 1 < words.Length)
							for (int i = 0; i < spaces; i++)
								s.Append (' ');
						if (extras > 0) {
							//s.Append ('_');
							extras--;
						}
					}
					return ustring.Make (s.ToString ());
				}
				return str;
			}
		}

		void Recalc ()
		{
			recalcPending = false;
			Recalc (text, lines, Frame.Width, textAlignment);
		}

		static void Recalc (ustring textStr, List<ustring> lineResult, int width, TextAlignment talign)
		{
			lineResult.Clear ();
			if (textStr.IndexOf ('\n') == -1) {
				lineResult.Add (ClipAndJustify (textStr, width, talign));
				return;
			}
			int textLen = textStr.Length;
			int lp = 0;
			for (int i = 0; i < textLen; i++) {
				Rune c = textStr [i];

				if (c == '\n') {
					lineResult.Add (ClipAndJustify (textStr [lp, i], width, talign));
					lp = i + 1;
				}
			}
			lineResult.Add(ClipAndJustify(textStr[lp, textLen], width, talign));
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (recalcPending)
				Recalc ();

			if (TextColor != -1)
				Driver.SetAttribute (TextColor);
			else
				Driver.SetAttribute (ColorScheme.Normal);

			Clear ();
			for (int line = 0; line < lines.Count; line++) {
				if (line < bounds.Top || line > bounds.Bottom)
					continue;
				var str = lines [line];
				int x;
				switch (textAlignment) {
				case TextAlignment.Left:
					x = 0;
					break;
				case TextAlignment.Justified:
					Recalc ();
					x = Bounds.Left;
					break;
				case TextAlignment.Right:
					x = Bounds.Right - str.Length;
					break;
				case TextAlignment.Centered:
					x = Bounds.Left + (Bounds.Width - str.Length) / 2;
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				}
				Move (x, line);
				Driver.AddStr (str);
			}
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text by the <see cref="Label"/> view
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The width for the text.</param>
		public static int MeasureLines (ustring text, int width)
		{
			var result = new List<ustring> ();
			Recalc (text, result, width, TextAlignment.Left);
			return result.Count;
		}

		/// <summary>
		/// Computes the max width of a line or multilines needed to render by the Label control
		/// </summary>
		/// <returns>Max width of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The width for the text.</param>
		public static int MaxWidth(ustring text, int width)
		{
			var result = new List<ustring>();
			Recalc(text, result, width, TextAlignment.Left);
			return result.Max(s => s.RuneCount);
		}

		/// <summary>
		///   The text displayed by the <see cref="Label"/>.
		/// </summary>
		public virtual ustring Text {
			get => text;
			set {
				text = value;
				recalcPending = true;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Controls the text-alignment property of the label, changing it will redisplay the <see cref="Label"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment TextAlignment {
			get => textAlignment;
			set {
				textAlignment = value;
				SetNeedsDisplay ();
			}
		}

		Attribute textColor = -1;
		/// <summary>
		///   The color used for the <see cref="Label"/>.
		/// </summary>
		public Attribute TextColor {
			get => textColor;
			set {
				textColor = value;
				SetNeedsDisplay ();
			}
		}
	}

}

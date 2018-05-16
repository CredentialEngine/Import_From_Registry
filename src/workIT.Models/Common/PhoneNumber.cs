using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;
using System.Security.Permissions;
//using System.Text;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;

namespace workIT.Models.Common
{
	/// <summary>Represents a phone number in the NANP (North American Numbering Plan) format.</summary>
	/// <remarks>See http://en.wikipedia.org/wiki/North_American_Numbering_Plan for details on on the NANP phone number format.</remarks>
	/// <example>
	/// The following example shows how to create a new <see cref="PhoneNumber"/> and validate it to make sure it is a valid NANP phone number:
	/// <code>
	/// PhoneNumber phone = new PhoneNumber("1-800-MYPHONE", true);
	/// if (phone.IsNanpValid)
	/// {
	///     Console.Write("{0:G} is a valid NANP phone number.", phone);
	/// }
	/// else
	/// {
	///     Console.Write("{0:G} is NOT a valid NANP phone number.", phone);
	/// }
	/// </code>
	/// </example>
	[Serializable, TypeConverter( typeof( PhoneNumberConverter ) )]
	public struct PhoneNumber : IComparable, IFormattable, ISerializable
	{
		/// <summary>Represents an empty <see cref="PhoneNumber"/> instance.</summary>
		public static readonly PhoneNumber Empty = new PhoneNumber();

		private int _CountryCode;
		private int _NpaCode;
		private int _NxxCode;
		private int _StationCode;
		private int _Extension;
		private string _Phonetic;

		#region Constructors

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="npaCode">The 3 digit NPA code.</param>
		/// <param name="nxxCode">The 3 digit NXX code.</param>
		/// <param name="stationCode">The 4 digit station code.</param>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(800, 222, 2222);
		/// Console.WriteLine("{0:D}", phone); // output: (800) 222-2222
		/// </code>
		/// </example>
		public PhoneNumber( int npaCode, int nxxCode, int stationCode )
			: this( 1, npaCode, nxxCode, stationCode, 0 )
		{
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="countryCode">The country code. Defaults to 1 (US).</param>
		/// <param name="npaCode">The 3 digit NPA code.</param>
		/// <param name="nxxCode">The 3 digit NXX code.</param>
		/// <param name="stationCode">The 4 digit station code.</param>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:D}", phone); // output: 1-800-222-2222
		/// </code>
		/// </example>
		public PhoneNumber( int countryCode, int npaCode, int nxxCode, int stationCode )
			: this( countryCode, npaCode, nxxCode, stationCode, 0 )
		{
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="countryCode">The country code. Defaults to 1 (US).</param>
		/// <param name="npaCode">The 3 digit NPA code.</param>
		/// <param name="nxxCode">The 3 digit NXX code.</param>
		/// <param name="stationCode">The 4 digit station code.</param>
		/// <param name="extension">The extension code.</param>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222, 1234);
		/// Console.WriteLine("{0:D}", phone); // output: 1-800-222-2222 ext 1234
		/// </code>
		/// </example>
		public PhoneNumber( int countryCode, int npaCode, int nxxCode, int stationCode, int extension )
		{
			this._CountryCode = countryCode <= 0 ? 1 : countryCode;
			this._NpaCode = npaCode;
			this._NxxCode = nxxCode;
			this._StationCode = stationCode;
			this._Extension = extension;
			this._Phonetic = string.Empty;
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("1-800-222-2222 ext 1234");
		/// Console.WriteLine("{0:D}", phone); // output: 1-800-222-2222 ext 1234
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public PhoneNumber( string value )
			: this( value, false, CultureInfo.CurrentCulture )
		{
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("1-800-222-2222 ext 1234", System.Globalization.CultureInfo.CurrentCulture);
		/// Console.WriteLine("{0:D}", phone); // output: 1-800-222-2222 ext 1234
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public PhoneNumber( string value, IFormatProvider formatProvider )
			: this( value, false, formatProvider )
		{
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("1-800-MYPHONE ext 1234", true);
		/// Console.WriteLine("{0:(a) p E}", phone); // output: (800) MYPHONE ext 1234
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public PhoneNumber( string value, bool allowPhonetic )
			: this( value, allowPhonetic, CultureInfo.CurrentCulture )
		{
			return;
		}

		/// <summary>Creates a new <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example creates a new <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("1-800-MYPHONE ext 1234", true, System.Globalization.CultureInfo.CurrentCulture);
		/// Console.WriteLine("{0:(a) p E}", phone); // output: (800) MYPHONE ext 1234
		/// </code>
		/// </example>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public PhoneNumber( string value, bool allowPhonetic, IFormatProvider formatProvider )
		{
			PhoneNumber._Parse( ref value, out this._CountryCode, out this._NpaCode, out this._NxxCode, out this._StationCode, out this._Extension, out this._Phonetic, allowPhonetic, formatProvider );
			return;
		}

		#endregion

		/// <summary>Initializes a new instance of the <see cref="PhoneNumber"/> class with serialized data.</summary>
		/// <param name="info">A <see cref="SerializationInfo"/> object that contains the information required to serialize the <see cref="PhoneNumber"/> instance.</param>
		/// <param name="context">A <see cref="StreamingContext"/> structure that contains the source and destination of the serialized stream associated with the <see cref="PhoneNumber"/> instance.</param>
		/// <exception cref="ArgumentNullException"><b>info</b> is null.</exception>
		private PhoneNumber( SerializationInfo info, StreamingContext context )
		{
			if ( info == null )
			{
				throw new ArgumentNullException( "info" );
			}

			this._CountryCode = info.GetInt32( "_CountryCode" );
			this._NpaCode = info.GetInt32( "_NpaCode" );
			this._NxxCode = info.GetInt32( "_NxxCode" );
			this._StationCode = info.GetInt32( "_StationCode" );
			this._Extension = info.GetInt32( "_Extension" );
			this._Phonetic = info.GetString( "_Phonetic" );
			return;
		}

		/// <summary>Implements the <see cref="ISerializable"/> interface and returns the data needed to serialize the <see cref="PhoneNumber"/> instance.</summary>
		/// <param name="info">A <see cref="SerializationInfo"/> object that contains the information required to serialize the <see cref="PhoneNumber"/> instance.</param>
		/// <param name="context">A <see cref="StreamingContext"/> structure that contains the source and destination of the serialized stream associated with the <see cref="PhoneNumber"/> instance.</param>
		/// <exception cref="ArgumentNullException"><b>info</b> is null.</exception>
		[SecurityPermission( SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter )]
		public void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			if ( info == null )
			{
				throw new ArgumentNullException( "info" );
			}

			info.AddValue( "_CountryCode", this._CountryCode );
			info.AddValue( "_NpaCode", this._NpaCode );
			info.AddValue( "_NxxCode", this._NxxCode );
			info.AddValue( "_StationCode", this._StationCode );
			info.AddValue( "_Extension", this._Extension );
			info.AddValue( "_Phonetic", this._Phonetic );
			return;
		}

		#region Properties

		/// <summary>Gets a value indicating if this instance has any values.</summary>
		/// <remarks>
		/// Since the station code can be 0000, and the NPA, country, and extension codes are not required, this property will simply check to see if the
		/// NXX code is greater than zero, since the NXX code can not be 000.
		/// </remarks>
		public bool HasValues
		{
			get { return this.NxxCode > 0; }
		}

		/// <summary>Gets a value indicating if the phone number validates with the NANP number format (US, Canada, and several other North American countries).</summary>
		/// <remarks>See http://en.wikipedia.org/wiki/North_American_Numbering_Plan#Current_system for details on the current NANP format.</remarks>
		/// <example>
		/// The following example shows how to validate a phone number to make sure it is a valid NANP phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("1-800-MYPHONE", true);
		/// if (phone.IsNanpValid)
		/// {
		///     Console.Write("{0:G} is a valid NANP phone number.", phone);
		/// }
		/// else
		/// {
		///     Console.Write("{0:G} is NOT a valid NANP phone number.", phone);
		/// }
		/// </code>
		/// </example>
		public bool IsNanpValid
		{
			get
			{
				bool v_area_code = this.NpaCode == 0; // NPA (Numbering Plan Area code), value values: [2-9][0-8][0-9] (200-289, 300-999).
				bool v_prefix = this.NxxCode >= 200 && this.NxxCode <= 999; // NXX, or Central Office/Exchange code, valid values: [2-9][0-9][0-9] (200-999).
				bool v_number = this.StationCode >= 0 && this.StationCode <= 9999; // Station code, valid values: [0-9][0-9][0-9][0-9] (0-9999).

				if ( this.NpaCode >= 200 )
				{
					if ( this.NpaCode >= 300 && this.NpaCode <= 999 )
					{
						v_area_code = true;
					}
					else if ( this.NpaCode < 290 )
					{
						v_area_code = true;
					}
				}

				return v_area_code && v_prefix && v_number;
			}
		}

		/// <summary>Gets or sets the country code.</summary>
		/// <remarks>If a value less than or equal to zero or no country code is specified, the country code of "1" (US) is assumed.</remarks>
		/// <example>
		/// The following example sets the country code for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-222-2222
		/// phone.CountryCode = 2;
		/// Console.WriteLine("{0:G}", phone); // output: 2-800-222-2222
		/// </code>
		/// </example>
		public int CountryCode
		{
			get { return this._CountryCode; }
			set
			{
				if ( value <= 0 )
				{
					value = 1;
				}
				if ( value != this._CountryCode )
				{
					this._CountryCode = value;
				}
			}
		}

		/// <summary>Gets or sets the 3 digit NPA (Numbering Plan Area) code.</summary>
		/// <remarks>This property will accept any value between 0 and 999, however, valid NANP NPA codes are ranged from 200-289 and 300-999.</remarks>
		/// <example>
		/// The following example sets the NPA code for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-222-2222
		/// phone.NpaCode = 877;
		/// Console.WriteLine("{0:G}", phone); // output: 1-877-222-2222
		/// </code>
		/// </example>
		/// <exception cref="ArgumentOutOfRangeException"><b>value</b> is less than 0 or greater than 999.</exception>
		public int NpaCode
		{
			get { return this._NpaCode; }
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid NPA code. The value must be a value between (and including) 0 and 999." );
				}
				if ( value > 999 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid NPA code. The value must be a value between (and including) 0 and 999." );
				}
				if ( value != this._NpaCode )
				{
					this._NpaCode = value;
				}
			}
		}

		/// <summary>Gets or sets the 3 digit NXX (Central Office or Exchange) code.</summary>
		/// <remarks>This property will accept any value between 0 and 999, however valid NANP NXX codes are ranged from 200-999.</remarks>
		/// <example>
		/// The following example sets the NXX code for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-222-2222
		/// phone.NxxCode = 333;
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-333-2222
		/// </code>
		/// </example>
		/// <exception cref="ArgumentOutOfRangeException"><b>value</b> is less than 0 or greater than 999.</exception>
		public int NxxCode
		{
			get { return this._NxxCode; }
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid 3 digit NXX code. The value must be a value between (and including) 0 and 999." );
				}
				if ( value > 999 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid 3 digit NXX code. The value must be a value between (and including) 0 and 999." );
				}
				if ( value != this._NxxCode )
				{
					this._NxxCode = value;
				}
			}
		}

		/// <summary>Gets or sets the 4 digit station code.</summary>
		/// <example>
		/// The following example sets the station code for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-222-2222
		/// phone.StationCode = 3333;
		/// Console.WriteLine("{0:G}", phone); // output: 1-800-222-3333
		/// </code>
		/// </example>
		/// <exception cref="ArgumentOutOfRangeException"><b>value</b> is less than 0 or greater than 9999.</exception>
		public int StationCode
		{
			get { return this._StationCode; }
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid 4 digit station code. The value must be a value between (and including) 0 and 9999." );
				}
				if ( value > 9999 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid 4 digit station code. The value must be a value between (and including) 0 and 9999." );
				}
				if ( value != this._StationCode )
				{
					this._StationCode = value;
				}
			}
		}

		/// <summary>Gets or sets the 4 digit extension code.</summary>
		/// <example>
		/// The following example sets the extension code for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 222, 2222);
		/// Console.WriteLine("{0:P}", phone); // output: (800) 222-2222
		/// phone.Extension = 1234;
		/// Console.WriteLine("{0:P}", phone); // output: (800) 222-2222 ext 1234
		/// </code>
		/// </example>
		/// <exception cref="ArgumentOutOfRangeException"><b>value</b> is less than 0 or greater than 9999.</exception>
		public int Extension
		{
			get { return this._Extension; }
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid extension code. The value must be a value between (and including) 0 and 9999." );
				}
				if ( value > 9999 )
				{
					throw new ArgumentOutOfRangeException( "value", value, "Invalid extension code. The value must be a value between (and including) 0 and 9999." );
				}
				if ( value != this._Extension )
				{
					this._Extension = value;
				}
			}
		}

		/// <summary>Gets or sets the phone number as a phonetic string, if one is specified.</summary>
		/// <remarks>
		/// Phonetic phone numbers can not have more than 7 letters in them (to represent the NXX and station codes; extension, NPA, and country codes
		/// can not be phonetic). If more than 7 letters are specified then an exception will be thrown. For example: "(800) MYPHONE" or "1-800-MYPHONE"
		/// or "1-800-222-TEST" are all valid phonetic phone numbers.<br />
		/// <br />
		/// This property will only contain the phonetic part of the phone number. For example, if the full phone number is "1-800-MYPHONE", then this
		/// property will contain the value "MYPHONE". If the full phone number is "1-800-222-TEST", then this property will contain the value "TEST".<br />
		/// <br />
		/// When setting this property, the phonetic value is <b>NOT</b> validated against the numeric values contained in the <see cref="NxxCode"/> and
		/// <see cref="StationCode"/> properties. It is assumed the phonetic value being set can be converted to the numerical values.
		/// </remarks>
		/// <example>
		/// The following example sets the phonetic string for a phone number:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber(1, 800, 697, 4663);
		/// Console.WriteLine("{0:P}", phone); // output: (800) 697-4663
		/// phone.Phonetic = "MYPHONE";
		/// Console.WriteLine("{0:P}", phone); // output: (800) MYPHONE
		/// </code>
		/// </example>
		/// <exception cref="FormatException"><b>value</b> is more than 7 characters long or contains a non alpha-numeric character (a-z, A-Z, 0-9, and -).</exception>
		public string Phonetic
		{
			get { return this._Phonetic; }
			set
			{
				if ( value == null )
				{
					value = string.Empty;
				}
				if ( value != this._Phonetic )
				{
					char[] v_chars = value.ToCharArray();
					int v_counter = 0;
					foreach ( char c in v_chars )
					{
						int v_code = ( int ) c;
						// codes: 48-57 = 0-9, 65-90 = A-Z, 97-122 = a-z, 45 = -
						if ( ( v_code >= 48 && v_code <= 57 ) || ( v_code >= 65 && v_code <= 90 ) || ( v_code >= 97 && v_code <= 122 ) || v_code == 45 )
						{
							if ( v_code != 45 )
							{
								v_counter++;
								if ( v_counter > 7 )
								{
									// No need to continue looping if we are past the limit of 7 alpha-numeric characters.
									throw new FormatException( "The phonetic representation of a phone number can only contain alpha-numeric characters or the \"-\" character, and can have no more than seven alpha-numeric characters" );
								}
							}
							continue;
						}
						// No need to continue looping if the current character is not an accepted character.
						throw new FormatException( "The phonetic representation of a phone number can only contain alpha-numeric characters or the \"-\" character, and can have no more than seven alpha-numeric characters" );
					}
					this._Phonetic = value;
				}
			}
		}

		#endregion

		/// <summary>Resets the property values to their default values.</summary>
		/// <remarks>
		/// Resets all of the numerical values (country, NPA, NXX, station, and extension codes) to zero, and resets the phonetic string
		/// to an empty string.
		/// </remarks>
		/// <example>
		/// <code>
		/// PhoneNumber phone = new PhoneNumber("800 MYPHONE", true);
		/// Console.WriteLine("{0:F}", phone); // output: 1-800-MYPHONE (697-4663)
		/// phone.Reset();
		/// Console.WriteLine("{0:F}", phone); // output: [emtpy string]
		/// </code>
		/// </example>
		public void Reset()
		{
			this._CountryCode = 0;
			this._NpaCode = 0;
			this._NxxCode = 0;
			this._StationCode = 0;
			this._Extension = 0;
			this._Phonetic = string.Empty;
			return;
		}

		#region To/From String

		/// <summary>Returns a string that represents this object instance.</summary>
		/// <returns>A string that represents this object instance.</returns>
		public override string ToString()
		{
			return this.ToString( "P", CultureInfo.CurrentCulture );
		}

		/// <summary>Returns a string that represents this object instance.</summary>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <returns>A string that represents this object instance.</returns>
		public string ToString( IFormatProvider formatProvider )
		{
			return this.ToString( "P", formatProvider );
		}

		/// <summary>Returns a string that represents this object instance.</summary>
		/// <param name="format">The format string to use to format the phone number.</param>
		/// <returns>A string that represents this object instance.</returns>
		/// <remarks>See <see cref="PhoneNumber.ToString(string, IFormatProvider)"/> for more details about format strings.</remarks>
		public string ToString( string format )
		{
			return this.ToString( format, CultureInfo.CurrentCulture );
		}

		/// <summary>Returns a string that represents this object instance.</summary>
		/// <param name="format">The format string to use to format the phone number.</param>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <returns>A string that represents this object instance.</returns>
		/// <remarks>
		/// The following tokens are regonized:<br />
		/// <br />
		/// <table border="1" cellpadding="2">
		/// <tr><td><b>Token</b></td><td><b>Description</b></td></tr>
		/// <tr><td><b>D</b></td><td>Automatically formats the phone number based on its values. Possible formats: c-a-x-s E, c-a-x-s, (a) x-s, (a) x-s E, x-s E, and x-s.</td></tr>
		/// <tr><td><b>G</b></td><td>Alias of "c-a-x-s e".</td></tr>
		/// <tr><td><b>N</b></td><td>Plain numerical phone number, with no special formatting characters (same as caxs e and/or caxs and/or axs e and/or axs and/or xs e and/or xs).</td></tr>
		/// <tr><td><b>P</b></td><td>Phonetic representation, if a phonetic string is specified. If there is no phonetic string, this format string acts the same as the "D" format string. Possible formats: c-a-p E, c-a-p, (a) p E, (a) p, p E, p, c-a-x-s E, c-a-x-s, (a) x-s E, (a) x-s, x-s E, and x-s.</td></tr>
		/// <tr><td><b>F</b></td><td>Alias of "P (x-s)", or "D" if there is no phonetic string.</td></tr>
		/// <tr><td><b>c</b></td><td>Any place this character occurs in the format string will be replaced by the country code.</td></tr>
		/// <tr><td><b>a</b></td><td>Any place this character occurs in the format string will be replaced by the 3 digit NPA code.</td></tr>
		/// <tr><td><b>x</b></td><td>Any place this character occurs in the format string will be replaced by the 3 digit NXX code.</td></tr>
		/// <tr><td><b>s</b></td><td>Any place this character occurs in the format string will be replaced by the 4 digit station code.</td></tr>
		/// <tr><td><b>e</b></td><td>Any place this character occurs in the format string will be replaced by the extension code.</td></tr>
		/// <tr><td><b>E</b></td><td>Any place this character occurs in the format string will be replaced by the extension code prefixed by "ext ".</td></tr>
		/// <tr><td><b>p</b></td><td>Any place this character occurs in the format string will be replaced by the phonetic representaion of the phone number. If there is no phonetic string, this format string is the same as "x-s". If the phonetic string is not seven characters, then the NPA and NXX codes are used to make it seven characters.</td></tr>
		/// </table>
		/// <br />
		/// All other characters are left as-is. Note that tokens are cap-sensitive.<br />
		/// If no format string is specified (a <b>null</b> or empty value), then the <b>F</b> format string is used.
		/// </remarks>
		public string ToString( string format, IFormatProvider formatProvider )
		{
			if ( format == null )
			{
				format = "P";
			}
			else if ( format.Length <= 0 || !this.HasValues )
			{
				return string.Empty;
			}
			switch ( format )
			{
				case "D":
					format = this._GetDefaultFormatString();
					break;
				case "G":
					format = this.Extension > 0 ? "c-a-x-s e" : "c-a-x-s";
					break;
				case "N":
					format = this.CountryCode > 0 && this.NpaCode > 0 ? "caxs" : ( this.NpaCode > 0 ? "axs" : "xs" );
					if ( this.Extension > 0 )
					{
						format += " e";
					}
					break;
				case "P":
					format = string.IsNullOrEmpty( this.Phonetic ) ? this._GetDefaultFormatString() : this._GetPhoneticFormatString( true );
					break;
				case "F":
					if ( string.IsNullOrEmpty( this.Phonetic ) )
					{
						format = this._GetDefaultFormatString();
					}
					else
					{
						format = string.Format( formatProvider, "{0} (x-s)", this._GetPhoneticFormatString( false ) );
						if ( this.Extension > 0 )
						{
							format += " E";
						}
					}
					break;
			}

			string v_phonetic = string.Empty;
			if ( string.IsNullOrEmpty( this.Phonetic ) )
			{
				v_phonetic = string.Format( formatProvider, "{0:000}-{1:0000}", this.NxxCode, this.StationCode );
			}
			else
			{
				string v_stripped_phonetic = this.Phonetic == null ? string.Empty : this.Phonetic.Replace( "-", string.Empty );
				int v_length = v_stripped_phonetic.Length;
				if ( v_length == 7 )
				{
					v_phonetic = this.Phonetic;
				}
				else if ( v_length == 4 )
				{
					v_phonetic = string.Format( formatProvider, "{0:000}-{1}", this.NxxCode, this.Phonetic );
				}
				else if ( v_length < 4 )
				{
					v_phonetic = string.Format( formatProvider, "{0:000}-{1}{2}", this.NxxCode, this.StationCode.ToString( "0000", formatProvider ).Substring( 0, 4 - v_length ), this.Phonetic );
				}
				else if ( v_length < 7 )
				{
					v_phonetic = string.Format( formatProvider, "{0}{1}", this.NxxCode.ToString( "000", formatProvider ).Substring( 0, 7 - v_length ), this.Phonetic );
				}
				else
				{
					v_phonetic = string.Format( formatProvider, "{0:000}-{1:0000}", this.NxxCode, this.StationCode );
				}
			}

			format = format.Replace( "c", this.CountryCode.ToString( formatProvider ) )
				.Replace( "a", this.NpaCode.ToString( "000", formatProvider ) )
				.Replace( "x", this.NxxCode.ToString( "000", formatProvider ) )
				.Replace( "s", this.StationCode.ToString( "0000", formatProvider ) );

			if ( this.Extension > 0 )
			{
				format = format.Replace( "e", this.Extension.ToString( formatProvider ) )
					.Replace( "E", "ext " + this.Extension.ToString( formatProvider ) );
			}
			else
			{
				format = format.Replace( "e", string.Empty )
					.Replace( "E", string.Empty );
			}

			return format.Replace( "p", v_phonetic ).Trim();
		}

		/// <summary>Parses the specified string into a phone number.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <example>
		/// The following example parses a string into the a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber();
		/// try
		/// {
		///     phone.FromString("(800) 222-2222");
		///     Console.WriteLine("Phone number was parsed successfully!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("Phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public void FromString( string value )
		{
			this.FromString( value, false, CultureInfo.CurrentCulture );
			return;
		}

		/// <summary>Parses the specified string into a phone number.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <example>
		/// The following example parses a string into the a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber();
		/// try
		/// {
		///     phone.FromString("(800) MYPHONE", true);
		///     Console.WriteLine("Phone number was parsed successfully!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("Phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public void FromString( string value, bool allowPhonetic )
		{
			this.FromString( value, allowPhonetic, CultureInfo.CurrentCulture );
			return;
		}

		/// <summary>Parses the specified string into a phone number.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <example>
		/// The following example parses a string into the a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber();
		/// try
		/// {
		///     phone.FromString("800 222-2222", System.Globalization.CultureInfo.CurrentCulture);
		///     Console.WriteLine("Phone number was parsed successfully!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("Phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public void FromString( string value, IFormatProvider formatProvider )
		{
			this.FromString( value, false, formatProvider );
			return;
		}

		/// <summary>Parses the specified string into a phone number.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <param name="formatProvider">The format provider to use for formating operations.</param>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <example>
		/// The following example parses a string into the a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone = new PhoneNumber();
		/// try
		/// {
		///     phone.FromString("800 MYPHONE", true, System.Globalization.CultureInfo.CurrentCulture);
		///     Console.WriteLine("Phone number was parsed successfully!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("Phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public void FromString( string value, bool allowPhonetic, IFormatProvider formatProvider )
		{
			PhoneNumber._Parse( ref value, out this._CountryCode, out this._NpaCode, out this._NxxCode, out this._StationCode, out this._Extension, out this._Phonetic, allowPhonetic, formatProvider );
			return;
		}

		#endregion

		/// <summary>Returns the hash code for this instance.</summary>
		/// <returns>A hash code for this instance.</returns>
		public override int GetHashCode()
		{
			return this.CountryCode.GetHashCode()
				^ this.NpaCode.GetHashCode()
				^ this.NxxCode.GetHashCode()
				^ this.StationCode.GetHashCode()
				^ this.Extension.GetHashCode();
		}

		/// <summary>Determines whether the specified object instance is equal to this object instance.</summary>
		/// <param name="obj">The object instance to check for equality.</param>
		/// <returns>TRUE if obj is the same type, contains equal values, or is a reference to this object insatnce, otherwise, FALSE.</returns>
		public override bool Equals( object obj )
		{
			if ( obj == null )
			{
				return false;
			}
			if ( object.ReferenceEquals( this, obj ) == true )
			{
				return true;
			}
			if ( obj.GetType() == typeof( PhoneNumber ) )
			{
				PhoneNumber pn = ( PhoneNumber ) obj;
				return pn.CountryCode == this.CountryCode
					&& pn.NpaCode == this.NpaCode
					&& pn.NxxCode == this.NxxCode
					&& pn.StationCode == this.StationCode
					&& pn.Extension == this.Extension;
			}
			return false;
		}

		/// <summary>Copies the values of the specified instance to the this object instance.</summary>
		/// <param name="value">The object to copy the values from.</param>
		public void CopyFrom( PhoneNumber value )
		{
			this._CountryCode = value.CountryCode;
			this._NpaCode = value.NpaCode;
			this._NxxCode = value.NxxCode;
			this._StationCode = value.StationCode;
			this._Extension = value.Extension;
			this._Phonetic = value.Phonetic;
			return;
		}

		/// <summary>Compares the specifed object to this object instance and returns an indication of their relative values.</summary>
		/// <param name="value">The object to compare to.</param>
		/// <returns>Less that zero if this object instance is less that the specified object, greater than zero if this object instance is greater that the specified object, or zero if the two objects are equal.</returns>
		public int CompareTo( PhoneNumber value )
		{
			if ( value == PhoneNumber.Empty )
			{
				return 1;
			}
			int v_ccode = this.CountryCode.CompareTo( value.CountryCode );
			if ( v_ccode == 0 )
			{
				int v_npa = this.NpaCode.CompareTo( value.NpaCode );
				if ( v_npa == 0 )
				{
					int v_nxx = this.NxxCode.CompareTo( value.NxxCode );
					if ( v_nxx == 0 )
					{
						int v_scode = this.StationCode.CompareTo( value.StationCode );
						return v_scode == 0 ? this.Extension.CompareTo( value.Extension ) : v_scode;
					}
				}
				return v_npa;
			}
			return v_ccode;
		}

		int IComparable.CompareTo( object obj )
		{
			if ( obj == null )
			{
				return 1;
			}
			if ( obj.GetType() == typeof( PhoneNumber ) )
			{
				return this.CompareTo( ( PhoneNumber ) obj );
			}
			throw new ArgumentException( "Object must be of type System.PhoneNumber.", "obj" );
		}

		#region Strip and display methods
	/// <summary>
		/// Strip special characters from a phone number
		/// </summary>
		/// <param name="phone"></param>
		/// <returns></returns>
		public static string StripPhone( string phone )
		{
			if ( string.IsNullOrWhiteSpace( phone) )
				return "";

			//if phone starts with +, leave as is
			if ( phone.StartsWith( "+" ) )
				return phone;

			string workPhone = phone.Trim();
			workPhone = workPhone.Replace( "-", "" );
			workPhone = workPhone.Replace( " ", "" );
			workPhone = workPhone.Replace( "_", "" );
			workPhone = workPhone.Replace( ".", "" );
			workPhone = workPhone.Replace( "(", "" );
			workPhone = workPhone.Replace( ")", "" );

			return workPhone;
		}
		/// <summary>
		/// Display a formatted phone number
		/// </summary>
		/// <param name="phone"></param>
		/// <returns>a formatted phone number</returns>
		public static string DisplayPhone( string phone )
		{
			//use default format
			return DisplayPhone( 2, phone, "" );
		}
		/// <summary>
		/// Display a formatted phone number
		/// </summary>
		/// <param name="phone"></param>
		/// <param name="ext"></param>
		/// <returns>a formatted phone number</returns>
		public static string DisplayPhone( string phone, string ext = "" )
		{
			//use default format
			return DisplayPhone( 2, phone, ext );
		}

		/// <summary>
		/// Display a formatted phone number
		/// </summary>
		/// <param name="displayFormat"></param>
		/// <param name="phone"></param>
		/// <param name="ext"></param>/// 
		/// <returns></returns>
		public static string DisplayPhone( int displayFormat, string phone, string ext )
		{
			if ( string.IsNullOrWhiteSpace( phone ))
				return "";
			if ( phone.Length == 3 )
				return phone;
			//phones are stored as received, so may want to just return as is
			if ( phone.IndexOf("(") > -1 || phone.IndexOf( "-" ) > -1 )
				return phone;

			string part1 = "";
			string part2 = "";
			string part3 = "";
			string prefix = "";
			//may want to strip first, just in case
			phone = StripPhone( phone );
			//phone = phone.Replace( "-", "" ).Replace( "(", "" ).Trim();
			//if phone starts with +, leave as is
			if ( phone.StartsWith( "+" ) )
				return phone;

			if ( phone.Length > 9 )
			{
				if ( phone.Length == 10 )
				{
					part1 = phone.Substring( 0, 3 );
					part2 = phone.Substring( 3, 3 );
					part3 = phone.Substring( 6, 4 );
				}
				else if ( phone.Length == 11 )
				{
					prefix = phone.Substring( 0, 1 );
					part1 = phone.Substring( 1, 3 );
					part2 = phone.Substring( 4, 3 );
					part3 = phone.Substring( 7, 4 );
				}
				else if ( phone.Length > 11 )
				{
					//hmm, could take last 7, and rest as prefix

					prefix = phone.Substring( 0, phone.Length - 10 );
					part1 = phone.Substring( phone.Length - 10, 3 );
					part2 = phone.Substring( phone.Length - 7, 3 );
					part3 = phone.Substring( phone.Length - 4, 4 );
				}

				return DisplayPhone( displayFormat, part1, part2, part3, ext, prefix );
			}
			else
			{
				return "";
			}
		}
		/// <summary>
		/// Display a formatted phone number
		/// </summary>
		/// <param name="displayFormat"></param>
		/// <param name="part1"></param>/// 
		/// <param name="part2"></param>
		/// <param name="part3"></param>
		/// <param name="ext"></param>
		/// <returns></returns>
		private static string DisplayPhone( int displayFormat, string part1, string part2, string part3, string ext, string prefix = "" )
		{
			string phone;

			if ( displayFormat == 1 )
			{
				phone = "(" + part1 + ") ";
				phone += part2 + "-";
				phone += part3;

			}
			else if ( displayFormat == 2 )
			{
				phone = part1 + "-" + part2 + "-" + part3;
			}
			else
			{
				phone = "(" + part1 + ") ";
				phone += part2 + "-";
				phone += part3;
			}

			if ( !string.IsNullOrWhiteSpace( ext ) && ext.Length > 0 )
			{
				phone += " ext: " + ext;
			}
			if ( !string.IsNullOrWhiteSpace( prefix ) && prefix.Length > 0 )
			{
				phone = prefix + "-" + phone;
			}

			return phone;
		}
		#endregion

		#region Static Methods
	/// <summary>Returns a value indicating if the two instances are equal to eachother.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the two instances are equal, otherwise, FALSE.</returns>
		public static bool operator ==( PhoneNumber left, PhoneNumber right )
		{
			if ( object.ReferenceEquals( left, null ) )
			{
				return object.ReferenceEquals( right, null );
			}
			return left.Equals( right );
		}

		/// <summary>Returns a value indicating if the two instances are not equal to eachother.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the two instances are not equal, otherwise, FALSE.</returns>
		public static bool operator !=( PhoneNumber left, PhoneNumber right )
		{
			return !( left == right );
		}

		/// <summary>Returns a value indicating if the left instance is greater than the right instance.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the left instance is greater than the right instance, otherwise, FALSE.</returns>
		public static bool operator >( PhoneNumber left, PhoneNumber right )
		{
			return left.CompareTo( right ) > 0;
		}

		/// <summary>Returns a value indicating if the left instance is less than the right instance.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the left instance is less than the right instance, otherwise, FALSE.</returns>
		public static bool operator <( PhoneNumber left, PhoneNumber right )
		{
			return left.CompareTo( right ) < 0;
		}

		/// <summary>Returns a value indicating if the left instance is greater than or equal to the right instance.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the left instance is greater than or equal to the right instance, otherwise, FALSE.</returns>
		public static bool operator >=( PhoneNumber left, PhoneNumber right )
		{
			return left.CompareTo( right ) >= 0;
		}

		/// <summary>Returns a value indicating if the left instance is less than or equal to the right instance.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		/// <returns>TRUE if the left instance is less than or equal to the right instance, otherwise, FALSE.</returns>
		public static bool operator <=( PhoneNumber left, PhoneNumber right )
		{
			return left.CompareTo( right ) <= 0;
		}

		/// <summary>Creates a new PhoneNumber instance by parsing the specified string.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <returns>A new phone number instance with values set to the parsed string.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// try
		/// {
		///     PhoneNumber phone = PhoneNumber.Parse("(800) 222-2222");
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public static PhoneNumber Parse( string value )
		{
			return new PhoneNumber( value, false, CultureInfo.CurrentCulture );
		}

		/// <summary>Creates a new PhoneNumber instance by parsing the specified string.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <returns>A new phone number instance with values set to the parsed string.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// try
		/// {
		///     PhoneNumber phone = PhoneNumber.Parse("(800) MYPHONE", true);
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public static PhoneNumber Parse( string value, bool allowPhonetic )
		{
			return new PhoneNumber( value, allowPhonetic, CultureInfo.CurrentCulture );
		}

		/// <summary>Creates a new PhoneNumber instance by parsing the specified string.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="provider">The format provider to use for formating operations.</param>
		/// <returns>A new phone number instance with values set to the parsed string.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// try
		/// {
		///     PhoneNumber phone = PhoneNumber.Parse("800 222-2222", System.Globalization.CultureInfo.CurrentCulture);
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		public static PhoneNumber Parse( string value, IFormatProvider provider )
		{
			return new PhoneNumber( value, false, provider );
		}

		/// <summary>Creates a new PhoneNumber instance by parsing the specified string.</summary>
		/// <param name="value">The phone number string to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <param name="provider">The format provider to use for formating operations.</param>
		/// <returns>A new phone number instance with values set to the parsed string.</returns>
		/// <remarks>
		/// If <b>allowPhonetic</b> is true, then phonetic phone numbers (such as 1-800-MYPHONE) will be allowed.
		/// Any character that is not a numeric character (0-9) is stripped from the string, unless <b>allowPhonetic</b> is true, in which case any
		/// character that is not an alpha-numeric character (0-9 and A-Z) is stripped from the string. The string must be, at a minimum, 7 characters
		/// long, or an exception will be thrown.<br />
		/// <br />
		/// Phonetic phone numbers can not have more than 7 letters in them (to represent the NXX and station codes; extension, NPA, and country codes
		/// can not be phonetic). If more than 7 letters are specified then an exception will be thrown.
		/// For example: "(800) MYPHONE" or "1-800-MYPHONE" or "1-800-222-TEST" are all valid phonetic phone numbers.
		/// 
		/// Extension codes are regonized when they are separated from the phone number with either "ext" or "ex".
		/// For example: "1-800-222-2222ex1234" or "1-800-222-2222 ex1234" or "1-800-222-2222 ex 1234" will regonize "1234" as an extension. 
		/// </remarks>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// try
		/// {
		///     PhoneNumber phone = PhoneNumber.Parse("800 MYPHONE", true, System.Globalization.CultureInfo.CurrentCulture);
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// catch (FormatException)
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException"><b>value</b> is null or empty.</exception>
		/// <exception cref="FormatException"><b>value</b> could not be parsed into a valid phone number.</exception>
		public static PhoneNumber Parse( string value, bool allowPhonetic, IFormatProvider provider )
		{
			return new PhoneNumber( value, allowPhonetic, provider );
		}

		/// <summary>Tries to parse the specified string into a phone number.</summary>
		/// <param name="value">The value to attempt to parse.</param>
		/// <param name="number">The PhoneNumber value of the parsed value.</param>
		/// <returns>TRUE on success, otherwise, FALSE.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone;
		/// if (PhoneNumber.TryParse("(800) 222-2222", out phone))
		/// {
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// else
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public static bool TryParse( string value, out PhoneNumber number )
		{
			return PhoneNumber.TryParse( value, false, CultureInfo.CurrentCulture, out number );
		}

		/// <summary>Tries to parse the specified string into a phone number.</summary>
		/// <param name="value">The value to attempt to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <param name="number">The PhoneNumber value of the parsed value.</param>
		/// <returns>TRUE on success, otherwise, FALSE.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone;
		/// if (PhoneNumber.TryParse("(800) MYPHONE", true, out phone))
		/// {
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// else
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public static bool TryParse( string value, bool allowPhonetic, out PhoneNumber number )
		{
			return PhoneNumber.TryParse( value, allowPhonetic, CultureInfo.CurrentCulture, out number );
		}

		/// <summary>Tries to parse the specified string into a phone number.</summary>
		/// <param name="value">The value to attempt to parse.</param>
		/// <param name="provider">The format provider to use for formating operations.</param>
		/// <param name="number">The PhoneNumber value of the parsed value.</param>
		/// <returns>TRUE on success, otherwise, FALSE.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone;
		/// if (PhoneNumber.TryParse("800 222-2222", System.Globalization.CultureInfo.CurrentCulture, out phone))
		/// {
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// else
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public static bool TryParse( string value, IFormatProvider provider, out PhoneNumber number )
		{
			return PhoneNumber.TryParse( value, false, provider, out number );
		}

		/// <summary>Tries to parse the specified string into a phone number.</summary>
		/// <param name="value">The value to attempt to parse.</param>
		/// <param name="allowPhonetic">
		/// A value indicating if letters are allowed in the phone number string. Any letters will be converted to their number equivilant and
		/// the phonetic representation will be stored in the <see cref="PhoneNumber.Phonetic"/> property.
		/// </param>
		/// <param name="provider">The format provider to use for formating operations.</param>
		/// <param name="number">The PhoneNumber value of the parsed value.</param>
		/// <returns>TRUE on success, otherwise, FALSE.</returns>
		/// <remarks>See <see cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/> for details about parsing a string to a phone number.</remarks>
		/// <seealso cref="PhoneNumber.Parse(string, bool, IFormatProvider)"/>
		/// <example>
		/// The following example parses a phone number string into a <see cref="PhoneNumber"/> instance:
		/// <code>
		/// PhoneNumber phone;
		/// if (PhoneNumber.TryParse("800 MYPHONE", true, System.Globalization.CultureInfo.CurrentCulture, out phone))
		/// {
		///     Console.WriteLine("Phone number successfully parsed!");
		/// }
		/// else
		/// {
		///     Console.WriteLine("The phone number could not be parsed!");
		/// }
		/// </code>
		/// </example>
		public static bool TryParse( string value, bool allowPhonetic, IFormatProvider provider, out PhoneNumber number )
		{
			try
			{
				number = new PhoneNumber( value, allowPhonetic, provider );
				return true;
			}
			catch ( FormatException ) { }
			catch ( ArgumentNullException ) { }
			number = new PhoneNumber();
			return false;
		}

		#endregion

		private string _GetDefaultFormatString()
		{
			string v_format = this.CountryCode > 1 && this.NpaCode > 0 ? "c-a-x-s" : ( this.NpaCode > 0 ? "(a) x-s" : "x-s" );
			if ( this.Extension > 0 )
			{
				v_format += " E";
			}
			return v_format;
		}

		private string _GetPhoneticFormatString( bool autoAddExtension )
		{
			string v_format = this.CountryCode > 1 && this.NpaCode > 0 ? "c-a-p" : ( this.NpaCode > 0 ? "(a) p" : "p" );
			if ( autoAddExtension && this.Extension > 0 )
			{
				v_format += " E";
			}
			return v_format;
		}

		// Characters that are allowed (but will be skipped/removed) besides alpha-numeric characters and the '-' character.
		private const string ALLOWED_CHARS = " ().[]{}|\\/_";

		/// <summary>
		/// Parse a passed Phone number. Will throw a FormatException on any errors or incorrect format. The calling method needs to handle this.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="countryCode"></param>
		/// <param name="npaCode"></param>
		/// <param name="nxxCode"></param>
		/// <param name="stationCode"></param>
		/// <param name="extension"></param>
		/// <param name="phonetic"></param>
		/// <param name="allowPhonetic"></param>
		/// <param name="formatProvider"></param>
		private static void _Parse( ref string value,
																		out int countryCode,
																		out int npaCode,
																		out int nxxCode,
																		out int stationCode,
																		out int extension,
																		out string phonetic,
																		bool allowPhonetic,
																		IFormatProvider formatProvider )
		{
			countryCode = 0;
			npaCode = 0;
			nxxCode = 0;
			stationCode = 0;
			extension = 0;
			phonetic = string.Empty;

			if ( string.IsNullOrEmpty( value ) )
			{
				throw new ArgumentNullException( "value" );
			}

			value = value.ToUpperInvariant();

			// Parse out the extension code if there is one.
			string v_ext_spliter = string.Empty;
			if ( value.Contains( "EXT" ) )
			{
				v_ext_spliter = "EXT";
			}
			else if ( value.Contains( "EX" ) )
			{
				v_ext_spliter = "EX";
			}

			int v_code;

			// There could possibly be an extension, lets try to parse it out.
			if ( !string.IsNullOrEmpty( v_ext_spliter ) )
			{
				string[] v_ext_parts = value.Split( new string[] { v_ext_spliter }, StringSplitOptions.RemoveEmptyEntries );
				int v_ext_length = v_ext_parts.Length;

				if ( v_ext_length == 2 )
				{
					// We have an extension string, parse it to it's numerical format. Only numerical extensions are supported.
					string v_ext_string = v_ext_parts[ 1 ];

					if ( !string.IsNullOrEmpty( v_ext_string ) )
					{
						v_ext_string = v_ext_string.Replace( v_ext_spliter, string.Empty ).Trim();
						StringBuilder v_ext_sb = new StringBuilder( v_ext_string.Length );

						foreach ( char v_char in v_ext_string )
						{
							v_code = ( int ) v_char;

							if ( v_code >= 48 && v_code <= 57 )
							{
								v_ext_sb.Append( v_char );
							}
						}

						int v_ext = 0;
						int.TryParse( v_ext_sb.ToString(), NumberStyles.Integer, formatProvider, out v_ext );
						if ( v_ext > 0 )
						{
							extension = v_ext;

							// We no longer need the extension part of the number.
							value = v_ext_parts[ 0 ];
							if ( !string.IsNullOrEmpty( value ) )
							{
								value = value.Trim();
							}
						}
					}
				}
			}

			// First lets strip all non-numerical values from the string, and store any letter and "-" characters into a separate string.
			StringBuilder v_sb = new StringBuilder( value.Length );
			StringBuilder v_alpha = new StringBuilder( value.Length );
			int v_counter = 0;

			foreach ( char c in value.ToCharArray() )
			{
				v_code = ( int ) c;
				if ( v_code >= 48 && v_code <= 57 ) // 0-9 characters
				{
					v_sb.Append( c );
					continue;
				}
				else if ( allowPhonetic )
				{
					if ( ( v_code >= 65 && v_code <= 90 ) || ( v_code >= 97 && v_code <= 122 ) ) // a-z and A-Z characters
					{
						v_sb.Append( PhoneNumber._ConvertToNumericalChar( v_code ) );
						if ( v_counter < 7 )
						{
							v_alpha.Append( c );
						}
						v_counter++;
						if ( v_counter > 7 )
						{
							throw new FormatException( "The phonetic representation of a phone number can have no more than seven characters from the following character set: [a-zA-Z]." );
						}
					}
					else if ( v_code == 45 ) // "-" character
					{
						v_alpha.Append( c );
					}
					continue;
				}
				else if ( v_code == 45 ) // "-" character
				{
					v_alpha.Append( c );

				}
				else if ( ALLOWED_CHARS.IndexOf( c ) < 0 )
				{
					throw new FormatException( "Invalid character detected. Only the following non alpha-numeric characters are allowed: () .[]{}|\\/_" );
				}
			}

			//check results
			string v_number = v_sb.ToString();
			phonetic = v_alpha.ToString();
			// Remove any "-" from the beginning of the phonetic string.
			while ( phonetic.IndexOf( '-' ) == 0 )
			{
				phonetic = phonetic.Substring( 1 );
			}

			// Is the resulting string long enough to be a phone number without an area code?
			int v_length = v_number.Length;
			bool v_format_error = false;
			if ( v_length < 7 )
			{
				v_format_error = true;
			}

		// We have enough digits to make a phone number without an area code.
			else if ( v_length == 7 )
			{
				countryCode = 1;
				npaCode = 0;
				try
				{
					nxxCode = int.Parse( allowPhonetic ? v_number.Substring( 0, 3 ) : v_number.Substring( 0, 3 ), formatProvider );
					stationCode = int.Parse( allowPhonetic ? v_number.Substring( 3, 4 ) : v_number.Substring( 3, 4 ), formatProvider );
				}
				catch ( FormatException )
				{
					v_format_error = true;
				}
				catch ( OverflowException )
				{
					v_format_error = true;
				}
				catch ( ArgumentException )
				{
					v_format_error = true;
				}
			}

		// We have enough digits to make a phone number with an area code.
			else if ( v_length <= 10 )
			{
				countryCode = 1;

				try
				{
					npaCode = int.Parse( v_number.Substring( 0, 3 ), formatProvider );
					nxxCode = int.Parse( allowPhonetic ? v_number.Substring( 3, 3 ) : v_number.Substring( 3, 3 ), formatProvider );
					stationCode = int.Parse( allowPhonetic ? v_number.Substring( 6, 4 ) : v_number.Substring( 6, 4 ), formatProvider );
				}
				catch ( FormatException )
				{
					v_format_error = true;
				}
				catch ( OverflowException )
				{
					v_format_error = true;
				}
				catch ( ArgumentException )
				{
					v_format_error = true;
				}
			}

			// We have a country code as well as an area code.
			else
			{
				try
				{
					countryCode = int.Parse( v_number.Substring( 0, v_length - 10 ), formatProvider );
					npaCode = int.Parse( v_number.Substring( v_length - 10, 3 ), formatProvider );
					nxxCode = int.Parse( allowPhonetic ? v_number.Substring( v_length - 7, 3 ) : v_number.Substring( v_length - 7, 3 ), formatProvider );
					stationCode = int.Parse( allowPhonetic ? v_number.Substring( v_length - 4, 4 ) : v_number.Substring( v_length - 4, 4 ), formatProvider );
				}
				catch ( FormatException )
				{
					v_format_error = true;
				}
				catch ( OverflowException )
				{
					v_format_error = true;
				}
				catch ( ArgumentException )
				{
					v_format_error = true;
				}
			}

			// A formatting error occured, so lets throw the format exception.
			if ( v_format_error )
			{
				throw new FormatException( string.Format( formatProvider, "The passed value ({0}) can not be parsed into a valid phone number.", value ) );
			}
			return;
		}

		private static char _ConvertToNumericalChar( int charCode )
		{
			switch ( charCode )
			{
				case 65:
				case 66:
				case 67:
				case 97:
				case 98:
				case 99:
					return '2';
				case 68:
				case 69:
				case 70:
				case 100:
				case 101:
				case 102:
					return '3';
				case 71:
				case 72:
				case 73:
				case 103:
				case 104:
				case 105:
					return '4';
				case 74:
				case 75:
				case 76:
				case 106:
				case 107:
				case 108:
					return '5';
				case 77:
				case 78:
				case 79:
				case 109:
				case 110:
				case 111:
					return '6';
				case 80:
				case 81:
				case 82:
				case 83:
				case 112:
				case 113:
				case 114:
				case 115:
					return '7';
				case 84:
				case 85:
				case 86:
				case 116:
				case 117:
				case 118:
					return '8';
				case 87:
				case 88:
				case 89:
				case 90:
				case 119:
				case 120:
				case 121:
				case 122:
					return '9';
			}
			return '\0'; // NULL character.
		}
	}

	#region PhoneNumberConverter

	internal class PhoneNumberConverter : ExpandableObjectConverter
	{
		public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
		{
			if ( sourceType == typeof( string ) )
			{
				return true;
			}
			return base.CanConvertFrom( context, sourceType );
		}

		public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
		{
			if ( value == null )
			{
				return new PhoneNumber();
			}
			if ( value is string )
			{
				string s = value as string;
				if ( s.Length <= 0 )
				{
					return new PhoneNumber();
				}
				return new PhoneNumber( s, true, culture );
			}
			throw new FormatException( "Can not convert to type System.PhoneNumber." );
		}

		public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
		{
			if ( ( destinationType == typeof( string ) ) | ( destinationType == typeof( InstanceDescriptor ) ) )
			{
				return true;
			}
			return base.CanConvertTo( context, destinationType );
		}

		public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
		{
			if ( value != null )
			{
				if ( value.GetType() != typeof( PhoneNumber ) )
				{
					throw new ArgumentException( "Invalid object type, expected type: System.PhoneNumber.", "value" );
				}
			}

			if ( destinationType == typeof( string ) )
			{
				if ( value == null )
				{
					return string.Empty;
				}
				PhoneNumber p = ( PhoneNumber ) value;
				return p.ToString( culture );
			}

			if ( destinationType == typeof( InstanceDescriptor ) )
			{
				if ( value == null )
				{
					return null;
				}
				PhoneNumber p = ( PhoneNumber ) value;
				MemberInfo member = null;
				object[] arguments = null;

				member = typeof( PhoneNumber ).GetConstructor( new Type[] { typeof( string ), typeof( bool ), typeof( IFormatProvider ) } );
				arguments = new object[] { p.ToString( culture ), true, culture };

				if ( member != null )
				{
					return new InstanceDescriptor( member, arguments, true );
				}
				else
				{
					return null;
				}
			}
			return base.ConvertTo( context, culture, value, destinationType );
		}
	}

	#endregion
}

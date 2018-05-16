﻿using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace workIT.Utilities
{
	/// <summary>
	/// Implementation of JsonPointer for Newtonsoft
	/// https://github.com/tavis-software/Tavis.JsonPointer
	/// </summary>
	public class JsonPointer
	{
		private readonly string[] _Tokens;

		public JsonPointer( string pointer )
		{
			_Tokens = pointer.Split( '/' ).Skip( 1 ).Select( Decode ).ToArray();
		}

		private JsonPointer( string[] tokens )
		{
			_Tokens = tokens;
		}
		private string Decode( string token )
		{
			return Uri.UnescapeDataString( token ).Replace( "~1", "/" ).Replace( "~0", "~" );
		}

		public bool IsNewPointer()
		{
			return _Tokens.Last() == "-";
		}

		public JsonPointer ParentPointer
		{
			get
			{
				if ( _Tokens.Length == 0 )
					return null;
				return new JsonPointer( _Tokens.Take( _Tokens.Length - 1 ).ToArray() );
			}
		}

		/// <summary>
		/// Find pointer in a Json token
		/// </summary>
		/// <param name="sample"></param>
		/// <returns></returns>
		public JToken Find( JToken sample )
		{
			if ( _Tokens.Length == 0 )
			{
				return sample;
			}
			try
			{
				var pointer = sample;
				foreach ( var token in _Tokens )
				{
					if ( pointer is JArray )
					{
						pointer = pointer[ Convert.ToInt32( token ) ];
					}
					else
					{
						pointer = pointer[ token ];
						if ( pointer == null )
						{
							throw new ArgumentException( "Cannot find " + token );
						}

					}
				}
				return pointer;
			}
			catch ( Exception ex )
			{
				throw new ArgumentException( "Failed to dereference pointer", ex );
			}
		}

		/// <summary>
		/// Find pointer in a Json string
		/// </summary>
		/// <param name="sample"></param>
		/// <returns></returns>
		public JToken Find( string sample )
		{
			if ( _Tokens.Length == 0 )
			{
				return sample;
			}
			try
			{
				JToken tokenized = JToken.Parse( sample );
				var pointer = tokenized;
				foreach ( var token in _Tokens )
				{
					if ( pointer is JArray )
					{
						pointer = pointer[ Convert.ToInt32( token ) ];
					}
					else
					{
						pointer = pointer[ token ];
						if ( pointer == null )
						{
							throw new ArgumentException( "Cannot find " + token );
						}

					}
				}
				return pointer;
			}
			catch ( Exception ex )
			{
				throw new ArgumentException( "Failed to dereference pointer", ex );
			}
		}

		public override string ToString()
		{
			return "/" + String.Join( "/", _Tokens );
		}
	}
}

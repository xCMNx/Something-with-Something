using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Palmmedia.GitHistory.Core.Model
{
	/// <summary>
	/// Represents a commit with child and parent relationships.
	/// </summary>
	internal class GitCommit : IGitCommit
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitCommit" /> class.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="title">The first line of the message..</param>
		/// <param name="message">The message.</param>
		/// <param name="autor">The autor of the commit</param>
		/// <param name="date">The date of the commit</param>
		public GitCommit(string id, string title, string message, string autor, DateTime date)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}

			Title = title;
			Autor = autor;
			Id = id;
			Message = string.Format("{0}: {1}", this.ShortId, FormatMessage(message ?? string.Empty));
			Date = date;
			Parents = new HashSet<IGitCommit>();
			Children = new HashSet<GitCommit>();
		}

		public string Autor { get; protected set; }
		public string Title { get; protected set; }
		public object Identifier => Id;

		/// <summary>
		/// Gets the identifier.
		/// </summary>
		/// <value>
		/// The identifier.
		/// </value>
		public string Id { get; private set; }

		/// <summary>
		/// Gets the short identifier.
		/// </summary>
		/// <value>
		/// The short identifier.
		/// </value>
		public string ShortId
		{
			get
			{
				return this.Id.Substring(0, 7);
			}
		}

		/// <summary>
		/// Gets the formatted commit message.
		/// </summary>
		/// <value>
		/// The message.
		/// </value>
		public string Message { get; private set; }

		/// <summary>
		/// Gets or sets the date of the commit.
		/// </summary>
		/// <value>
		/// The date of the commit.
		/// </value>
		public DateTime Date { get; set; }

		/// <summary>
		/// Gets the parents of the commit.
		/// </summary>
		/// <value>
		/// The parents.
		/// </value>
		public ICollection<IGitCommit> Parents { get; private set; }

		/// <summary>
		/// Gets the children of the commit.
		/// </summary>
		/// <value>
		/// The children.
		/// </value>
		public ICollection<GitCommit> Children { get; private set; }

		/// <summary>
		/// Gets or sets the name of the branch (if available).
		/// </summary>
		/// <value>
		/// The name of the branch.
		/// </value>
		public string BranchName { get; set; }

		/// <summary>
		/// Gets or sets the corresponding <see cref="MergedCommit"/> the commit belongs to.
		/// </summary>
		/// <value>
		/// The merged commit.
		/// </value>
		public MergedCommit MergedCommit { get; set; }

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			GitCommit other = obj as GitCommit;

			if (other == null)
			{
				return false;
			}

			return this.Id.Equals(other.Id);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return this.Id;
		}

		/// <summary>
		/// Formats the given commit message.
		/// All special characters are removed and line breaks are added to avoid long lines.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <returns>The formatted message.</returns>
		private static string FormatMessage(string message)
		{
			string line = Regex.Replace(message, "[^\\w^\\s]", string.Empty);

			if (line.Length < 50)
			{
				return line;
			}

			string result = string.Empty;
			string currentLine = string.Empty;

			foreach (var word in line.Split(' '))
			{
				if (currentLine.Length == 0 || currentLine.Length + word.Length < 50)
				{
					currentLine += word + " ";
				}
				else
				{
					result += currentLine;
					result += "\\n";
					currentLine = word + " ";
				}
			}

			result += currentLine;

			return result;
		}
	}
}

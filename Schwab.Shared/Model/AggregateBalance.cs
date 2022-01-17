namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Configuration;


    /// <summary>
    /// Aggregate Balance.
    /// </summary>
    public class AggregateBalance
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param id="id">Id.</param>
        public AggregateBalance(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param id="id">Id.</param>
        /// <param name="name">Name.</param>
        public AggregateBalance(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param id="id">Id.</param>
        /// <param name="name">Name.</param>
        /// <param balance="balance">Balance.</param>
        public AggregateBalance(int id, string name, Decimal balance)
        {
            Id = id;
            Name = name;
            Balance = balance;
        }

        /// <summary>
        /// Name.
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Name.
        /// </summary>
        public Decimal Balance { get; set; }


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} [id={1}, name={2}, aggregate balance={3:C}]", typeof(Client).Name, Id, Name, Balance);
        }

        /// <summary>
        /// Get string representation of collection.
        /// </summary>
        /// <returns></returns>
        private static string CollectionToString<T>(ICollection<T> col)
        {
            if (col == null)
                return "null";

            var elements = col.Any()
                ? col.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y)
                : string.Empty;

            return string.Format("[{0}]", elements);
        }
    }
}
using Render.TempFromVessel.Kernel;

namespace Render.Repositories.Kernel
{
    public interface IDataPersistence<T> : IDisposable where T : IDomainEntity
	{
		int Limit { get; set; }

        /// <summary>
        /// Returns the query as an domain model.
        /// </summary>
        /// <param name="searchField">The search field.</param>
        /// <param name="value">The value.</param>
        /// <param name="caseSensitive">true, if case sensitive</param>
        /// <returns>
        /// Single domain model.
        /// </returns>
        Task<T> QueryOnFieldAsync(string searchField, string value, bool caseSensitive = true, bool waitForIndex = false);

        /// <summary>
        /// Returns the query as a list of domain models.
        /// </summary>
        /// <param name="searchField">The search field.</param>
        /// <param name="value">The value.</param>
        /// <param name="limit">The limit. Passing 0 will give all results.</param>
        /// <param name="caseSensitive">true, if case sensitive</param>
        /// <returns>
        /// List of domain models.
        /// </returns>
        Task<List<T>> QueryOnFieldAsync(string searchField, string value, int limit, bool caseSensitive = true, bool waitForIndex = false);

        /// <summary>
        /// Returns the query result as a list of domain models.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>
        /// List of domain models.
        /// </returns>
        Task<List<T>> QueryOnFieldsAsync(bool waitForIndex = false, params Tuple<string, object>[] args);

		/// <summary>
		/// Gets the c# object asynchronously.
		/// </summary>
		/// <param name="id">The id used to build the key for the document</param>
		/// <returns>The C# object</returns>
		Task<T> GetAsync(Guid id);

		/// <summary>
		/// Gets the c# object asynchronously.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The C# object</returns>
		Task<T> GetAsync(string key);

		/// <summary>
		/// Upserts the json string.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="item">C# Object</param>
		Task UpsertAsync(Guid id, T item);

		/// <summary>
		/// Upserts the json string.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="item">C# Object</param>
		Task UpsertAsync(string key, T item);

		/// <summary>
		/// Get all of this type of object
		/// </summary>
		/// <param name="limit">If 0, return all results without limit</param>
		/// <param name="waitForIndex">if true, tells couchbase to wait for all current indexing operations before running the query.</param>
		/// <returns>
		/// A list of objects
		/// </returns>
		Task<List<T>> GetAllOfTypeAsync(int limit = 0, bool waitForIndex = false);

		Task<List<T>> ReturnTypeSubStringQueryAsStringAsync(string searchField, string value);

        Task<BatchStatus> BatchInsertAsync(List<T> list, bool forceFailure = false);

		Task DeleteAsync(Guid id);

		Task DeleteAsync(string key);

        Task<BatchStatus> BatchDeleteAsync(List<Guid> guids, bool forceFailure = false);

        Task PurgeAllOfTypeForProjectId(Guid projectId);

        Task ResetCreatedFromAudioIds(Guid projectId, List<Guid> deletedStandardQuestions);
	}
}

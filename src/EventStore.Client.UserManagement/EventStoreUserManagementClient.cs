using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Users;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The client used for operations on internal users.
	/// </summary>
	public class EventStoreUserManagementClient : EventStoreClientBase {
		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="EventStoreUserManagementClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public EventStoreUserManagementClient(EventStoreClientSettings? settings = null) :
			base(settings, ExceptionMap) {
			_log = Settings.LoggerFactory?.CreateLogger<EventStoreUserManagementClient>() ??
			       new NullLogger<EventStoreUserManagementClient>();
		}

		/// <summary>
		/// Creates an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="fullName"></param>
		/// <param name="groups"></param>
		/// <param name="password"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task CreateUserAsync(string loginName, string fullName, string[] groups, string password,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (loginName == null) throw new ArgumentNullException(nameof(loginName));
			if (fullName == null) throw new ArgumentNullException(nameof(fullName));
			if (groups == null) throw new ArgumentNullException(nameof(groups));
			if (password == null) throw new ArgumentNullException(nameof(password));
			if (loginName == string.Empty) throw new ArgumentOutOfRangeException(nameof(loginName));
			if (fullName == string.Empty) throw new ArgumentOutOfRangeException(nameof(fullName));
			if (password == string.Empty) throw new ArgumentOutOfRangeException(nameof(password));

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					LoginName = loginName,
					FullName = fullName,
					Password = password,
					Groups = {groups}
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the <see cref="UserDetails"/> of an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task<UserDetails> GetUserAsync(string loginName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (loginName == null) {
				throw new ArgumentNullException(nameof(loginName));
			}

			if (loginName == string.Empty) {
				throw new ArgumentOutOfRangeException(nameof(loginName));
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).Details(new DetailsReq {
				Options = new DetailsReq.Types.Options {
					LoginName = loginName
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			await call.ResponseStream.MoveNext().ConfigureAwait(false);
			var userDetails = call.ResponseStream.Current.UserDetails;
			return ConvertUserDetails(userDetails);
		}

		private static UserDetails ConvertUserDetails(DetailsResp.Types.UserDetails userDetails) =>
			new UserDetails(userDetails.LoginName, userDetails.FullName, userDetails.Groups.ToArray(),
				userDetails.Disabled, userDetails.LastUpdated?.TicksSinceEpoch.FromTicksSinceEpoch());

		/// <summary>
		/// Deletes an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task DeleteUserAsync(string loginName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (loginName == null) {
				throw new ArgumentNullException(nameof(loginName));
			}

			if (loginName == string.Empty) {
				throw new ArgumentOutOfRangeException(nameof(loginName));
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).DeleteAsync(new DeleteReq {
				Options = new DeleteReq.Types.Options {
					LoginName = loginName
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Enables a previously disabled internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task EnableUserAsync(string loginName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (loginName == null) {
				throw new ArgumentNullException(nameof(loginName));
			}

			if (loginName == string.Empty) {
				throw new ArgumentOutOfRangeException(nameof(loginName));
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).EnableAsync(new EnableReq {
				Options = new EnableReq.Types.Options {
					LoginName = loginName
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Disables an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task DisableUserAsync(string loginName, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (loginName == string.Empty) throw new ArgumentOutOfRangeException(nameof(loginName));

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).DisableAsync(new DisableReq {
				Options = new DisableReq.Types.Options {
					LoginName = loginName
				}
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Lists the <see cref="UserDetails"/> of all internal users.
		/// </summary>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async IAsyncEnumerable<UserDetails> ListAllAsync(UserCredentials? userCredentials = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).Details(new DetailsReq(),
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			await foreach (var userDetail in call.ResponseStream
				.ReadAllAsync(cancellationToken)
				.Select(x => ConvertUserDetails(x.UserDetails))
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false)) {
				yield return userDetail;
			}
		}

		/// <summary>
		/// Changes the password of an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="currentPassword"></param>
		/// <param name="newPassword"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task ChangePasswordAsync(string loginName, string currentPassword, string newPassword,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			if (loginName == null) throw new ArgumentNullException(nameof(loginName));
			if (currentPassword == null) throw new ArgumentNullException(nameof(currentPassword));
			if (newPassword == null) throw new ArgumentNullException(nameof(newPassword));
			if (loginName == string.Empty) throw new ArgumentOutOfRangeException(nameof(loginName));
			if (currentPassword == string.Empty) throw new ArgumentOutOfRangeException(nameof(currentPassword));
			if (newPassword == string.Empty) throw new ArgumentOutOfRangeException(nameof(newPassword));

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).ChangePasswordAsync(
				new ChangePasswordReq {
					Options = new ChangePasswordReq.Types.Options {
						CurrentPassword = currentPassword,
						NewPassword = newPassword,
						LoginName = loginName
					}
				},
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Resets the password of an internal user.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="newPassword"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task ResetPasswordAsync(string loginName, string newPassword,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			if (loginName == null) throw new ArgumentNullException(nameof(loginName));
			if (newPassword == null) throw new ArgumentNullException(nameof(newPassword));
			if (loginName == string.Empty) throw new ArgumentOutOfRangeException(nameof(loginName));
			if (newPassword == string.Empty) throw new ArgumentOutOfRangeException(nameof(newPassword));

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			var call = new Users.Users.UsersClient(
				channelInfo.CallInvoker).ResetPasswordAsync(
				new ResetPasswordReq {
					Options = new ResetPasswordReq.Types.Options {
						NewPassword = newPassword,
						LoginName = loginName
					}
				},
				EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		private static readonly IDictionary<string, Func<RpcException, Exception>> ExceptionMap =
			new Dictionary<string, Func<RpcException, Exception>> {
				[Constants.Exceptions.UserNotFound] = ex => new UserNotFoundException(
					ex.Trailers.First(x => x.Key == Constants.Exceptions.LoginName).Value),
			};
	}
}

﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using VirtoCommerce.Foundation.Data.Infrastructure;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Security;
using VirtoCommerce.Foundation.Security.Services;
using VirtoCommerce.ManagementClient.Core.Infrastructure;
using VirtoCommerce.ManagementClient.Core.Infrastructure.Dialogs;
using VirtoCommerce.ManagementClient.Security.Model;
using VirtoCommerce.ManagementClient.Security.ViewModel.Interfaces;

namespace VirtoCommerce.ManagementClient.Security.ViewModel.Implementations
{
	public class LoginViewModel : ViewModelBase, ILoginViewModel
	{
		#region Private fields

		private readonly IUnityContainer _container;
		private bool _isUserAuthenticated;

		#endregion

		public override bool IsBackTrackingDisabled
		{
			get
			{
				return true;
			}
		}

		#region Constructor

		public LoginViewModel(IUnityContainer container)
		{
			LoginCommand = new DelegateCommand<object>(ProcessLogin, CanLogin);
			_container = container;
			LoadValues();
#if DEBUG
			OnUIThread(() =>
				{
					if (string.IsNullOrEmpty(UserName))
					{
						UserName = SecurityModule.UserNameAdmin;
					}
					if (string.IsNullOrEmpty(BaseUrl))
					{
						BaseUrl =
							GetConnectionStringBaseUrl(SecurityConfiguration.Instance.Connection.DataServiceBaseUriName);
					}
					Password = "store";

				});
#endif
		}

		#endregion

		#region ILoginViewModel Members

		public DelegateCommand<object> LoginCommand { get; private set; }

		private bool _authProgress;
		public bool AuthProgress
		{
			get
			{
				return _authProgress;
			}
			private set
			{
				_authProgress = value;
				OnPropertyChanged("IsAnimation");
				OnPropertyChanged();
			}
		}

		private string _userName;
		public string UserName
		{
			get { return _userName; }
			set
			{
				_userName = value;
				OnPropertyChanged();
				Password = null;
				LoginCommand.RaiseCanExecuteChanged();
			}
		}

		private string _password;
		public string Password
		{
			get { return _password; }
			set
			{
				_password = value;
				OnPropertyChanged();
				LoginCommand.RaiseCanExecuteChanged();
			}
		}

		private string _baseUrl;
		public string BaseUrl
		{
			get
			{
				return _baseUrl;
			}
			set
			{
				_baseUrl = value;
				OnPropertyChanged();
				LoginCommand.RaiseCanExecuteChanged();
			}
		}

		private string _currentUserName;
		public string CurrentUserName
		{
			get { return _currentUserName; }
			set
			{
				_currentUserName = value;
				OnPropertyChanged();
			}
		}

		public bool IsAnimation
		{
			get { return _isUserAuthenticated || AuthProgress; }
		}

		private string _error;
		public string Error
		{
			get
			{
				return _error;
			}
			set
			{
				_error = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Private methods

		private bool CanLogin(object arg)
		{
			return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(BaseUrl);
		}

		private void RegisterSecurityServices(string serviceBaseUrl)
		{
			var factory = new ServiceConnectionFactory(serviceBaseUrl);

			_container.RegisterInstance<IServiceConnectionFactory>(factory, new ContainerControlledLifetimeManager());

			_container.RegisterService<ISecurityService>(
				factory.GetConnectionString(SecurityConfiguration.Instance.Connection.ServiceUri),
				"SecurityServiceConnectionEndPoint");

			_container.RegisterService<IAuthenticationService>(
				factory.GetConnectionString(SecurityConfiguration.Instance.Authentication.ServiceUri),
				SecurityConfiguration.Instance.Authentication.WSEndPointName);

			var service = _container.Resolve<IAuthenticationService>();
			_container.RegisterInstance<IAuthenticationContext>(new AuthenticationContext(service), new ContainerControlledLifetimeManager());
		}

		public event EventHandler LogonViewRequestedEvent;

		private async void ProcessLogin(object obj)
		{
			if (AuthProgress)
			{
				return;
			}
			AuthProgress = true;
			Error = null;
			CurrentUserName = null;
			try
			{

				var serviceBaseUrl = BaseUrl.ToLower();

				if (!serviceBaseUrl.EndsWith("/"))
				{
					serviceBaseUrl += "/";
				}

				if (!serviceBaseUrl.EndsWith("Virto/"))
				{
					serviceBaseUrl += "Virto/";
				}


				RegisterSecurityServices(serviceBaseUrl);
				var authenticationContext = _container.Resolve<IAuthenticationContext>();
				_isUserAuthenticated = await Task.Run(() => authenticationContext.Login(UserName, Password, serviceBaseUrl));

				if (_isUserAuthenticated)
				{
					OnPropertyChanged("IsAnimation");
					CurrentUserName = authenticationContext.CurrentUserName;
					var logonEvent = LogonViewRequestedEvent;
					if (logonEvent != null)
					{
						logonEvent(this, null);
					}
					CurrentUserName = string.Format("{0}", UserName);
					try
					{
						SaveValues();
					}
					catch
					{
					}
				}
			}
			catch (GetTokenException e)
			{
				Error = e.Message;
				AuthProgress = false;
			}
			catch (Exception e)
			{
				ErrorDialog.ShowErrorDialog(e.Message, e.StackTrace, e.ToString(), false, "Error at login. ");
				AuthProgress = false;
			}
		}

		#endregion

		#region Save/Load authentitication values

		private void SaveValues()
		{
			try
			{
				if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password) && !string.IsNullOrEmpty(_baseUrl))
				{
					var globalConfigService = _container.Resolve<IGlobalConfigService>();
					if (globalConfigService != null)
					{
						globalConfigService.Update("UserName", _userName);
						globalConfigService.Update("BaseUrl", _baseUrl);
					}

					// try getting settings from passed arguments
					var args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
					if (args != null && args.Length > 0)
					{
						_baseUrl = args[0];
					}
				}
			}
			catch
			{
			}
		}

		private void LoadValues()
		{
			try
			{
				string parameterUserName = null;
				string parameterBaseUrl = null;

				// Get the ActivationArguments from the SetupInformation property of the domain.
				var activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
				if (activationData != null && activationData.Length > 0)
				{
					parameterBaseUrl = activationData[0];
				}

				var globalConfigService = _container.Resolve<IGlobalConfigService>();
				if (globalConfigService != null)
				{
					parameterUserName = (string)globalConfigService.Get("UserName");
					parameterBaseUrl = parameterBaseUrl ?? (string)globalConfigService.Get("BaseUrl");
				}

				if (!string.IsNullOrEmpty(parameterUserName) || !string.IsNullOrEmpty(parameterBaseUrl))
				{
					OnUIThread(() =>
					{
						UserName = parameterUserName;
						BaseUrl = parameterBaseUrl;
					});
				}
			}
			catch
			{
			}
		}

		private static string GetConnectionStringBaseUrl(string baseUriName)
		{
			return ConfigurationManager.ConnectionStrings[baseUriName] != null ? ConfigurationManager.ConnectionStrings[baseUriName].ConnectionString : "http://";
		}

		#endregion


	}
}

﻿using System;
using System.IO;
using Microsoft.Azure.NotificationHubs.Auth;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Azure.NotificationHubs.DotNetCore.Tests
{
    public class ManagementApiE2ETests
    {
        private const string IncorrectConnectionString = "Endpoint=sb://sample-test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SharedAccessKey";
        private const string NotificationHubNamespaceUriString = "NotificationHubNamespaceUriString";
        private const string NotificationHubConnectionString = "NotificationHubConnectionString";
        private const string NotificationHubName = "NotificationHubName";
        private NamespaceManager _namespaceManager;
        private NamespaceManagerSettings _namespaceManagerSettings;
        private readonly string _notificationHubName;
        private string _namespaceUriString;
        private string _notificationHubConnectionString;

        public ManagementApiE2ETests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _namespaceUriString = Environment.GetEnvironmentVariable(NotificationHubNamespaceUriString.ToUpper()) ?? configuration[NotificationHubNamespaceUriString];
            _notificationHubConnectionString = Environment.GetEnvironmentVariable(NotificationHubConnectionString.ToUpper()) ?? configuration[NotificationHubConnectionString];
            _notificationHubName = Environment.GetEnvironmentVariable(NotificationHubName.ToUpper()) ?? configuration[NotificationHubName];

            _namespaceManagerSettings = new NamespaceManagerSettings();
            _namespaceManagerSettings.TokenProvider = SharedAccessSignatureTokenProvider.CreateSharedAccessSignatureTokenProvider(_notificationHubConnectionString);
            _namespaceManager = new NamespaceManager(new Uri(_namespaceUriString), _namespaceManagerSettings);
        }

        [Fact]
        public void ExecuteCreateGetAndDeleteNotificationHubMethods_ShouldCreateGetOrDeleteHubOrRecieveConflictException()
        {
            CleanUp();

            // Check that GetNotification returns MessagingEntityNotFoundException than hub is not exist
            Assert.Throws<MessagingEntityNotFoundException>(() => _namespaceManager.GetNotificationHub(_notificationHubName));

            // Check that CreateNotificationHub method create hub with correct Path
            var createNotificationHubDescription = _namespaceManager.CreateNotificationHub(_notificationHubName);
            Assert.Equal(_notificationHubName, createNotificationHubDescription.Path);

            // Check that CreateNotificationHub returns MessagingEntityAlreadyExistsException than hub is alredy exist
            Assert.Throws<MessagingEntityAlreadyExistsException>(() => _namespaceManager.CreateNotificationHub(_notificationHubName));

            // Check that GetNotificationHub returns correct hub
            var getNotificationHubDescription = _namespaceManager.GetNotificationHub(_notificationHubName);
            Assert.Equal(_notificationHubName, getNotificationHubDescription.Path);

            // Check that DeleteNotificationHub correctly remove hub
            _namespaceManager.DeleteNotificationHub(_notificationHubName);

            // Check that DeleteNotificationHub returns MessagingEntityNotFoundException than hub is not exist
            Assert.Throws<MessagingEntityNotFoundException>(() => _namespaceManager.DeleteNotificationHub(_notificationHubName));

            CleanUp();
        }

        [Fact]
        public void ExecuteCreateGetAndDeleteNotificationHubMethodsWithIncorrectConnectionString_ShouldRecieveUnauthorizedAccessException()
        {
            CleanUp();

            var namespaceManagerSettings = new NamespaceManagerSettings();
            namespaceManagerSettings.TokenProvider 
                = SharedAccessSignatureTokenProvider.CreateSharedAccessSignatureTokenProvider(IncorrectConnectionString);

            var namespaceManager = new NamespaceManager(new Uri(_namespaceUriString), namespaceManagerSettings);

            // Check that CreateNotificationHub returns UnauthorizedAccessException when connection string is incorrect
            Assert.Throws<UnauthorizedAccessException>(() => namespaceManager.CreateNotificationHub(_notificationHubName));

            // We must create hub to recieve UnauthorizedAccessException when GetNotificationHub and DeleteNotificationHub execute
            _namespaceManager.CreateNotificationHub(_notificationHubName);

            // Check that GetNotificationHub returns UnauthorizedAccessException when connection string is incorrect
            Assert.Throws<UnauthorizedAccessException>(() => namespaceManager.GetNotificationHub(_notificationHubName));

            // Check that DeleteNotificationHub returns UnauthorizedAccessException when connection string is incorrect
            Assert.Throws<UnauthorizedAccessException>(() => namespaceManager.DeleteNotificationHub(_notificationHubName));

            CleanUp();
        }
        
        private void CleanUp()
        {
            try
            {
                _namespaceManager.DeleteNotificationHub(_notificationHubName);
            }
            catch
            {
                // no-op
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.WindowsAzure.Commands.ScenarioTest;
using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;
using Microsoft.Rest.ClientRuntime.Azure.TestFramework;
using System.Collections.Generic;
using Microsoft.Azure.Test.HttpRecorder;
using Microsoft.Azure.Commands.Common.Authentication.Models;
using System.Reflection;
using System.IO;
using Microsoft.Azure.Management.Internal.Resources;
using Microsoft.Azure.ServiceManagement.Common.Models;
using Microsoft.Azure.Management.ContainerService;
using Microsoft.Azure.Management.Authorization.Version2015_07_01;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Graph.RBAC.Version1_6;
using Microsoft.Azure.Commands.Common.Authentication.Abstractions;

namespace Commands.Aks.Test.ScenarioTests
{
    public class TestController
    {
        private readonly EnvironmentSetupHelper _helper;

        public ContainerRegistryManagementClient ContainerRegistryManagementClient { get; private set; }

        public ContainerServiceClient ContainerServiceClient { get; private set; }

        public TestController()
        {
            _helper = new EnvironmentSetupHelper();
        }

        public static TestController NewInstance => new TestController();

        private const string TenantIdKey = "TenantId";
        private const string DomainKey = "Domain";
        private const string SubscriptionIdKey = "SubscriptionId";
        public string UserDomain { get; private set; }
        public GraphRbacManagementClient InternalGraphRbacManagementClient { get; private set; }

        public ResourceManagementClient InternalResourceManagementClient { get; private set; }

        public AuthorizationManagementClient InternalAuthorizationManagementClient { get; private set; }

        public void RunPowerShellTest(XunitTracingInterceptor logger, params string[] scripts)
        {
            var sf = new StackTrace().GetFrame(1);
            var callingClassType = sf.GetMethod().ReflectedType?.ToString();
            var mockName = sf.GetMethod().Name;

            _helper.TracingInterceptor = logger;

            var d = new Dictionary<string, string>
            {
                {"Microsoft.Resources", null},
                {"Microsoft.Features", null},
                {"Microsoft.Authorization", null}
            };
            var providersToIgnore = new Dictionary<string, string>
                {
                    {"Microsoft.Azure.Management.Resources.ResourceManagementClient", "2017-05-10"},
                    {"Microsoft.Azure.Management.ResourceManager.ResourceManagementClient", "2017-05-10"}
                };
            HttpMockServer.Matcher = new PermissiveRecordMatcherWithApiExclusion(false, d, providersToIgnore);
            HttpMockServer.RecordsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SessionRecords");

            using (var context = MockContext.Start(callingClassType, mockName))
            {
                SetupManagementClients(context);
                var callingClassName = callingClassType?.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last();

                _helper.SetupEnvironment(AzureModule.AzureResourceManager);
                _helper.SetupModules(AzureModule.AzureResourceManager,
                    _helper.RMProfileModule,
                    _helper.GetRMModulePath(@"AzureRM.Aks.psd1"),
                    _helper.GetRMModulePath(@"AzureRM.ContainerRegistry.psd1"),
                    "ScenarioTests\\Common.ps1",
                    "ScenarioTests\\" + callingClassName + ".ps1",
                    "AzureRM.Resources.ps1");

                if (HttpMockServer.GetCurrentMode() == HttpRecorderMode.Playback)
                {
                    AzureSession.Instance.DataStore = new MemoryDataStore();
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var dir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);
                    var subscription = HttpMockServer.Variables["SubscriptionId"];
                    AzureSession.Instance.DataStore.WriteFile(Path.Combine(home, ".ssh", "id_rsa.pub"), File.ReadAllText(dir + "/Fixtures/id_rsa.pub"));
                    var jsonOutput = @"{""" + subscription + @""":{ ""service_principal"":""foo"",""client_secret"":""bar""}}";
                    AzureSession.Instance.DataStore.WriteFile(Path.Combine(home, ".azure", "acsServicePrincipal.json"), jsonOutput);
                }
                else if (HttpMockServer.GetCurrentMode() == HttpRecorderMode.Record)
                {
                    AzureSession.Instance.DataStore = new MemoryDataStore();
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var dir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);
                    var subscription = HttpMockServer.Variables["SubscriptionId"];
                    var currentEnvironment = TestEnvironmentFactory.GetTestEnvironment();
                    string spn = null;
                    string spnSecret = null;
                    if (currentEnvironment.ConnectionString.KeyValuePairs.ContainsKey("ServicePrincipal"))
                    {
                        spn = currentEnvironment.ConnectionString.KeyValuePairs["ServicePrincipal"];
                    }
                    if (currentEnvironment.ConnectionString.KeyValuePairs.ContainsKey("ServicePrincipalSecret"))
                    {
                        spnSecret = currentEnvironment.ConnectionString.KeyValuePairs["ServicePrincipalSecret"];
                    }
                    AzureSession.Instance.DataStore.WriteFile(Path.Combine(home, ".ssh", "id_rsa.pub"), File.ReadAllText(dir + "/Fixtures/id_rsa.pub"));
                    var jsonOutput = @"{""" + subscription + @""":{ ""service_principal"":""" + spn + @""",""client_secret"":"""+ spnSecret + @"""}}";
                    AzureSession.Instance.DataStore.WriteFile(Path.Combine(home, ".azure", "acsServicePrincipal.json"), jsonOutput);
                }

                _helper.RunPowerShellTest(scripts);
            }
        }

        private void SetupManagementClients(MockContext context)
        {
            ContainerRegistryManagementClient = GetContainerRegistryManagementClient(context);
            ContainerServiceClient = GetContainerServiceClient(context);
            InternalResourceManagementClient = GetInternalResourceManagementClient(context);
            InternalAuthorizationManagementClient = GetAuthorizationManagementClient(context);
            InternalGraphRbacManagementClient = GetGraphRbacManagementClient(context);
            _helper.SetupManagementClients(ContainerRegistryManagementClient,
                ContainerServiceClient,
                InternalResourceManagementClient,
                InternalAuthorizationManagementClient,
                InternalGraphRbacManagementClient);
        }

        private static ContainerServiceClient GetContainerServiceClient(MockContext context)
        {
            return context.GetServiceClient<ContainerServiceClient>();
        }
        private GraphRbacManagementClient GetGraphRbacManagementClient(MockContext context)
        {
            //var environment = TestEnvironmentFactory.GetTestEnvironment();
            //string tenantId = null;

            //if (HttpMockServer.Mode == HttpRecorderMode.Record)
            //{
            //    tenantId = environment.Tenant;
            //    UserDomain = String.IsNullOrEmpty(environment.UserName) ? String.Empty : environment.UserName.Split(new[] { "@" }, StringSplitOptions.RemoveEmptyEntries).Last();

            //    HttpMockServer.Variables[TenantIdKey] = tenantId;
            //    HttpMockServer.Variables[DomainKey] = UserDomain;
            //}
            //else if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            //{
            //    if (HttpMockServer.Variables.ContainsKey(TenantIdKey))
            //    {
            //        tenantId = HttpMockServer.Variables[TenantIdKey];
            //    }
            //    if (HttpMockServer.Variables.ContainsKey(DomainKey))
            //    {
            //        UserDomain = HttpMockServer.Variables[DomainKey];
            //    }
            //    if (HttpMockServer.Variables.ContainsKey(SubscriptionIdKey))
            //    {
            //        AzureRmProfileProvider.Instance.Profile.DefaultContext.Subscription.Id = HttpMockServer.Variables[SubscriptionIdKey];
            //    }
            //}

            //var client = context.GetGraphServiceClient<GraphRbacManagementClient>(environment);
            //client.TenantID = tenantId;
            //if (AzureRmProfileProvider.Instance != null &&
            //    AzureRmProfileProvider.Instance.Profile != null &&
            //    AzureRmProfileProvider.Instance.Profile.DefaultContext != null &&
            //    AzureRmProfileProvider.Instance.Profile.DefaultContext.Tenant != null)
            //{
            //    AzureRmProfileProvider.Instance.Profile.DefaultContext.Tenant.Id = client.TenantID;
            //}
            //return client;
            return context.GetServiceClient<GraphRbacManagementClient>();
        }
        private static AuthorizationManagementClient GetAuthorizationManagementClient(MockContext context)
        {
            return context.GetServiceClient<AuthorizationManagementClient>();
        }
        private static ContainerRegistryManagementClient GetContainerRegistryManagementClient(MockContext context)
        {
            return context.GetServiceClient<ContainerRegistryManagementClient>();
        }
        private static ResourceManagementClient GetInternalResourceManagementClient(MockContext context)
        {
            return context.GetServiceClient<ResourceManagementClient>();
        }
    }
}
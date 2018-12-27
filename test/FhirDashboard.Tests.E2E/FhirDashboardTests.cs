using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FhirDashboard.Tests.E2E
{
    public class FhirDashboardTests
    {
        private IConfiguration _config;

        public FhirDashboardTests()
        {
            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<FhirDashboard.Tests.E2E.FhirDashboardTests>()
                .Build();
        }

        [Fact]
        public void DashboardLoginSuccessFull_and_TokenValidForFhirServer()
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["FhirServerUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUserUpn"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUserPassword"]));

            var options = new ChromeOptions();
            var dashboardUrl = _config["DashboardUrl"];

            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--incognito");

            // TODO: We are accepting insecure certs to make it practical to run on build systems. A valid cert should be on the build system.
            options.AcceptInsecureCertificates = true;

            // VSTS Hosted agents set the ChromeWebDriver Env, locally that is not the case
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/test/continuous-test-selenium?view=vsts#decide-how-you-will-deploy-and-test-your-app
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ChromeWebDriver")))
            {
                Environment.SetEnvironmentVariable("ChromeWebDriver", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }

            using (var driver = new ChromeDriver(Environment.GetEnvironmentVariable("ChromeWebDriver"), options))
            {
                // TODO: This parameter has been set (too) conservatively to ensure that content
                //       loads on build machines. Investigate if one could be less sensitive to that.
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                void Advance()
                {
                    while (true)
                    {
                        try
                        {
                            var button = driver.FindElementById("idSIButton9");
                            if (button.Enabled)
                            {
                                button.Click();
                                return;
                            }
                        }
                        catch (StaleElementReferenceException)
                        {
                        }
                    }
                }

                driver.Navigate().GoToUrl(_config["DashboardUrl"]);

                driver.SwitchTo().ActiveElement().SendKeys(_config["DashboardUserUpn"]);
                Advance();

                driver.FindElementByName("passwd").SendKeys(_config["DashboardUserPassword"]);
                Advance();

                driver.Navigate().GoToUrl($"{dashboardUrl}/Home/AboutMe");

                while (!driver.Url.StartsWith($"{dashboardUrl}/Home/AboutMe"))
                {

                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    var button = driver.FindElementById("idSIButton9");
                    if (button.Enabled)
                    {
                        button.Click();
                        return;
                    }
                }

                var element = driver.FindElement(By.Id("tokenfield"));
                String elementval = element.GetAttribute("value");

                var jwtHandler = new JwtSecurityTokenHandler();

                Assert.True(jwtHandler.CanReadToken(elementval));

                var token = jwtHandler.ReadJwtToken(elementval);
                var aud = token.Claims.Where(c => c.Type == "aud");

                Assert.Single(aud);

                var tokenAudience = aud.First().Value;

                Assert.Equal(_config["FhirServerUrl"], tokenAudience); 
            }
        }
    }
}

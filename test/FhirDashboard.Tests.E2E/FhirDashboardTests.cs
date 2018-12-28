using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        public async Task DashboardLoginSuccessFull_and_TokenValidForFhirServer()
        {
            Assert.True(!string.IsNullOrWhiteSpace(_config["FhirServerUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUrl"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUserUpn"]));
            Assert.True(!string.IsNullOrWhiteSpace(_config["DashboardUserPassword"]));

            var options = new ChromeOptions();
            var dashboardUrl = _config["DashboardUrl"];

            // We have to make sure the website is up
            var client = new HttpClient();
            var result =  await client.GetAsync(dashboardUrl);
            int waitCount = 0;
            while ((waitCount++ < 10) && !result.IsSuccessStatusCode)
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                result =  await client.GetAsync(dashboardUrl);
            }

            Assert.True(result.IsSuccessStatusCode);

            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--incognito");

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
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

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

                waitCount = 0;
                while (!driver.Url.StartsWith($"{dashboardUrl}/Home/AboutMe"))
                {

                    Thread.Sleep(TimeSpan.FromMilliseconds(5000));
                    var button = driver.FindElementById("idSIButton9");
                    if (button.Enabled)
                    {
                        button.Click();
                    }
                    Assert.InRange(waitCount++, 0, 10);
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

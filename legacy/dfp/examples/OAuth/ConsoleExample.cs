﻿// Copyright 2014, Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// Author: api.anash@gmail.com (Anash P. Oommen)

using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.Util.v201405;
using Google.Api.Ads.Dfp.v201405;
using Google.Api.Ads.Common.Lib;

using System;
using System.Data;

namespace Google.Api.Ads.Dfp.Examples.OAuth {
  /// <summary>
  /// This code example shows how to run an DFP API command line application
  /// while incorporating the OAuth2 installed application flow into your
  /// application. If your application uses a single Google login to make calls
  /// to all your accounts, you shouldn't use this code example. Instead, you
  /// should run Common\Util\OAuth2TokenGenerator.cs to generate a refresh token
  /// and set that in user.Config.OAuth2RefreshToken field, or set
  /// OAuth2RefreshToken key in your App.config / Web.config.
  ///
  /// This code example depends on Console environment only for reading and
  /// writing values, you may use this code example in other environments like
  /// Windows Form applications with minimial modifications.
  ///
  /// To run this application,
  ///
  /// 1. You should create a new Console Application project.
  /// 2. Add reference to the following assemblies:
  /// <list type="table">
  /// <item>Google.Ads.Common.dll</item>
  /// <item>Google.Ads.OAuth.dll</item>
  /// <item>Google.Dfp.dll</item>
  /// <item>System.Web</item>
  /// <item>System.Configuration</item>
  /// </list>
  /// 3. Replace the Main() method with this class's method.
  /// 4. Copy App.config from Dfp.Examples project, and configure
  /// it as shown in this project's Web.config.
  /// 5. Compile and run this example.
  /// </summary>
  public class ConsoleExample {
    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    static void Main(string[] args) {
      DfpUser user = new DfpUser();
      DfpAppConfig config = (user.Config as DfpAppConfig);
      if (config.AuthorizationMethod == DfpAuthorizationMethod.OAuth2) {
        if (config.OAuth2Mode == OAuth2Flow.APPLICATION &&
            string.IsNullOrEmpty(config.OAuth2RefreshToken)) {
          DoAuth2Authorization(user);
        }
      } else {
        throw new Exception("Authorization mode is not OAuth.");
      }

      // Get the UserService.
      UserService userService = (UserService)user.GetService(DfpService.v201405.UserService);

      // Create a Statement to get all users.
      StatementBuilder statementBuilder = new StatementBuilder()
          .OrderBy("id ASC")
          .Limit(StatementBuilder.SUGGESTED_PAGE_LIMIT);

      // Set default for page.
      UserPage page = new UserPage();

      try {
        do {
          // Get users by Statement.
          page = userService.getUsersByStatement(statementBuilder.ToStatement());

          if (page.results != null && page.results.Length > 0) {
            int i = page.startIndex;
            foreach (User usr in page.results) {
              Console.WriteLine("{0}) User with ID = '{1}', email = '{2}', and role = '{3}'" +
                  " was found.", i, usr.id, usr.email, usr.roleName);
              i++;
            }
          }
          statementBuilder.IncreaseOffsetBy(StatementBuilder.SUGGESTED_PAGE_LIMIT);
        } while (statementBuilder.GetOffset() < page.totalResultSetSize);

        Console.WriteLine("Number of results found: {0}", page.totalResultSetSize);
      } catch (Exception ex) {
        Console.WriteLine("Failed to get all users. Exception says \"{0}\"",
            ex.Message);
      }
    }

    /// <summary>
    /// Does the OAuth2 authorization for installed applications.
    /// </summary>
    /// <param name="user">The DFP user.</param>
    private static void DoAuth2Authorization(DfpUser user) {
      // Since we are using a console application, set the callback url to null.
      user.Config.OAuth2RedirectUri = null;
      AdsOAuthProviderForApplications oAuth2Provider =
          (user.OAuthProvider as AdsOAuthProviderForApplications);
      // Get the authorization url.
      string authorizationUrl = oAuth2Provider.GetAuthorizationUrl();
      Console.WriteLine("Open a fresh web browser and navigate to \n\n{0}\n\n. You will be " +
          "prompted to login and then authorize this application to make calls to the " +
          "DFP API. Once approved, you will be presented with an authorization code.",
          authorizationUrl);

      // Accept the OAuth2 authorization code from the user.
      Console.Write("Enter the authorization code :");
      string authorizationCode = Console.ReadLine();

      // Fetch the access and refresh tokens.
      oAuth2Provider.FetchAccessAndRefreshTokens(authorizationCode);
    }
  }
}

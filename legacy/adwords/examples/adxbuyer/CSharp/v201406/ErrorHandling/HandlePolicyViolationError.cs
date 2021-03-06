// Copyright 2014, Google Inc. All Rights Reserved.
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

using Google.Api.Ads.AdWords.Lib;
using Google.Api.Ads.AdWords.Util;
using Google.Api.Ads.AdWords.v201406;

using System;
using System.Collections.Generic;
using System.IO;

namespace Google.Api.Ads.AdWords.Examples.CSharp.v201406 {
  /// <summary>
  /// This code example adds a text ad, and shows how to handle a policy
  /// violation.
  ///
  /// Tags: AdGroupAdService.mutate
  /// </summary>
  public class HandlePolicyViolationError : ExampleBase {
    /// <summary>
    /// Main method, to run this code example as a standalone application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static void Main(string[] args) {
      HandlePolicyViolationError codeExample = new HandlePolicyViolationError();
      Console.WriteLine(codeExample.Description);
      try {
        long adGroupId = long.Parse("INSERT_ADGROUP_ID_HERE");
        codeExample.Run(new AdWordsUser(), adGroupId);
      } catch (Exception ex) {
        Console.WriteLine("An exception occurred while running this code example. {0}",
            ExampleUtilities.FormatException(ex));
      }
    }

    /// <summary>
    /// Returns a description about the code example.
    /// </summary>
    public override string Description {
      get {
        return "This code example adds a text ad, and shows how to handle a policy violation.";
      }
    }

    /// <summary>
    /// Runs the code example.
    /// </summary>
    /// <param name="user">The AdWords user.</param>
    /// <param name="adGroupId">Id of the ad group to which ads are added.
    /// </param>
    public void Run(AdWordsUser user, long adGroupId) {
      // Get the AdGroupAdService.
      AdGroupAdService service =
          (AdGroupAdService) user.GetService(AdWordsService.v201406.AdGroupAdService);

      // Create the third party redirect ad that violates a policy.
      ThirdPartyRedirectAd redirectAd = new ThirdPartyRedirectAd();
      redirectAd.name = "Policy violation demo ad " + ExampleUtilities.GetRandomString();
      redirectAd.url = "gopher://gopher.google.com";
      redirectAd.dimensions = new Dimensions();
      redirectAd.dimensions.width = 300;
      redirectAd.dimensions.height = 250;

      redirectAd.snippet = "<img src=\"https://sandbox.google.com/sandboximages/image.jpg\"/>";
      redirectAd.impressionBeaconUrl = "http://www.examples.com/beacon1";
      redirectAd.certifiedVendorFormatId = 119;
      redirectAd.isCookieTargeted = false;
      redirectAd.isUserInterestTargeted = false;
      redirectAd.isTagged = false;

      AdGroupAd redirectAdGroupAd = new AdGroupAd();
      redirectAdGroupAd.adGroupId = adGroupId;
      redirectAdGroupAd.ad = redirectAd;

      // Create the operations.
      AdGroupAdOperation redirectAdOperation = new AdGroupAdOperation();
      redirectAdOperation.@operator = Operator.ADD;
      redirectAdOperation.operand = redirectAdGroupAd;

      try {
        AdGroupAdReturnValue retVal = null;

        // Setup two arrays, one to hold the list of all operations to be
        // validated, and another to hold the list of operations that cannot be
        // fixed after validation.
        List<AdGroupAdOperation> allOperations = new List<AdGroupAdOperation>();
        List<AdGroupAdOperation> operationsToBeRemoved = new List<AdGroupAdOperation>();

        allOperations.Add(redirectAdOperation);

        try {
          // Validate the operations.
          service.RequestHeader.validateOnly = true;
          retVal = service.mutate(allOperations.ToArray());
        } catch (AdWordsApiException ex) {
          ApiException innerException = ex.ApiException as ApiException;
          if (innerException == null) {
            throw new Exception("Failed to retrieve ApiError. See inner exception for more " +
                "details.", ex);
          }

          // Examine each ApiError received from the server.
          foreach (ApiError apiError in innerException.errors) {
            int index = ErrorUtilities.GetOperationIndex(apiError.fieldPath);
            if (index == -1) {
              // This API error is not associated with an operand, so we cannot
              // recover from this error by removing one or more operations.
              // Rethrow the exception for manual inspection.
              throw;
            }

            // Handle policy violation errors.
            if (apiError is PolicyViolationError) {
              PolicyViolationError policyError = (PolicyViolationError) apiError;

              if (policyError.isExemptable) {
                // If the policy violation error is exemptable, add an exemption
                // request.
                List<ExemptionRequest> exemptionRequests = new List<ExemptionRequest>();
                if (allOperations[index].exemptionRequests != null) {
                  exemptionRequests.AddRange(allOperations[index].exemptionRequests);
                }

                ExemptionRequest exemptionRequest = new ExemptionRequest();
                exemptionRequest.key = policyError.key;
                exemptionRequests.Add(exemptionRequest);
                allOperations[index].exemptionRequests = exemptionRequests.ToArray();
              } else {
                // Policy violation error is not exemptable, remove this
                // operation from the list of operations.
                operationsToBeRemoved.Add(allOperations[index]);
              }
            } else {
              // This is not a policy violation error, remove this operation
              // from the list of operations.
              operationsToBeRemoved.Add(allOperations[index]);
            }
          }
          // Remove all operations that aren't exemptable.
          foreach (AdGroupAdOperation operation in operationsToBeRemoved) {
            allOperations.Remove(operation);
          }
        }

        if (allOperations.Count > 0) {
          // Perform the operations exemptible of a policy violation.
          service.RequestHeader.validateOnly = false;
          retVal = service.mutate(allOperations.ToArray());

          // Display the results.
          if (retVal != null && retVal.value != null && retVal.value.Length > 0) {
            foreach (AdGroupAd newAdGroupAd in retVal.value) {
              Console.WriteLine("New ad with id = \"{0}\" and displayUrl = \"{1}\" was created.",
                  newAdGroupAd.ad.id, newAdGroupAd.ad.displayUrl);
            }
          } else {
            Console.WriteLine("No ads were created.");
          }
        } else {
          Console.WriteLine("There are no ads to create after policy violation checks.");
        }
      } catch (Exception ex) {
        throw new System.ApplicationException("Failed to create ads.", ex);
      }
    }
  }
}

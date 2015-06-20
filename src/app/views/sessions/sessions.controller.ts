/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  class Session extends FlexSearch.DuplicateDetection.Session {
    JobStartTimeString: string
    JobEndTimeString: string
  }

  interface ISessionsProperties extends ng.IScope {
    Sessions: Session[]
    Limit: number
    Page: number
    Total: number
    goToSession(sessionId: string): void
  }

  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsProperties, $state: any, $http: ng.IHttpService) {
      $http.get(DuplicatesUrl + "/search?c=*&q=type+=+'session'").then((response: any) => {
        $scope.goToSession = function(sessionId) {
          $state.go('session', {sessionId: sessionId});
        };

        var toDateStr = function(dateStr: any) {
          var date = new Date(dateStr);
          return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
        }

        var results = <FlexSearch.Core.SearchResults>response.data.Data;
        $scope.Sessions = results.Documents
          .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
          .map(s => {
            s.JobStartTimeString = toDateStr(s.JobStartTime);
            s.JobEndTimeString = toDateStr(s.JobEndTime);
            return s;
          });
        $scope.Limit = 10;
        $scope.Page = 1;
        $scope.Total = results.TotalAvailable;
      });
    }
  }
}

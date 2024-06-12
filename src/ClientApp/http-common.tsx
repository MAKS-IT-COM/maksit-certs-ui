import { HttpService } from "./services/HttpService";

const httpService = new HttpService();

httpService.addRequestInterceptor(xhr => {

});

httpService.addResponseInterceptor(response => {

    return response;
});
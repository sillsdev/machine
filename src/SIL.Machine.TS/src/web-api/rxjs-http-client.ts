import { Observable } from 'rxjs';
import { ajax } from 'rxjs/ajax';
import { map } from 'rxjs/operators';
import { HttpClient, HttpResponse } from './http-client';

export class RxjsHttpClient extends HttpClient {
  constructor(private readonly baseUrl: string = '', private readonly accessToken: string = '') {
    super();
  }

  get<T>(url: string): Observable<HttpResponse<T>> {
    return ajax.get(this.getUrl(url), this.getHeaders()).pipe(map(res => ({ status: res.status, data: res.response })));
  }

  post<T>(url: string, body?: any): Observable<HttpResponse<T>> {
    return ajax
      .post(this.getUrl(url), body, this.getHeaders())
      .pipe(map(res => ({ status: res.status, data: res.response })));
  }

  protected getHeaders(): any {
    const headers = super.getHeaders();
    if (this.accessToken !== '') {
      headers.Authorization = 'Bearer ' + this.accessToken;
    }
    return headers;
  }

  private getUrl(url: string): string {
    let baseUrl = this.baseUrl;
    if (!baseUrl.endsWith('/')) {
      baseUrl += '/';
    }
    return baseUrl + url;
  }
}

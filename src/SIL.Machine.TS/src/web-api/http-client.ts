import { Observable } from 'rxjs';
import { ajax } from 'rxjs/ajax';
import { map } from 'rxjs/operators';

export interface HttpResponse<T> {
  status: number;
  data?: T;
}

export class HttpClient {
  constructor(private readonly baseUrl: string = '', private readonly accessToken: string = '') {}

  get<T>(url: string): Observable<HttpResponse<T>> {
    return ajax.get(this.getUrl(url), this.getHeaders()).pipe(map(res => ({ status: res.status, data: res.response })));
  }

  post<T>(url: string, body?: any): Observable<HttpResponse<T>> {
    return ajax
      .post(this.getUrl(url), body, this.getHeaders())
      .pipe(map(res => ({ status: res.status, data: res.response })));
  }

  private getUrl(url: string): string {
    let baseUrl = this.baseUrl;
    if (!baseUrl.endsWith('/')) {
      baseUrl += '/';
    }
    return baseUrl + url;
  }

  private getHeaders(): any {
    const headers: any = {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    };
    if (this.accessToken !== '') {
      headers.Authorization = 'Bearer ' + this.accessToken;
    }
    return headers;
  }
}

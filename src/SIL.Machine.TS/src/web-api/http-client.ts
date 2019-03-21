import { Observable } from 'rxjs';

export interface HttpResponse<T> {
  status: number;
  data?: T;
}

export abstract class HttpClient {
  abstract get<T>(url: string): Observable<HttpResponse<T>>;
  abstract post<T>(url: string, body?: any): Observable<HttpResponse<T>>;

  protected getHeaders(): any {
    return {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    };
  }
}

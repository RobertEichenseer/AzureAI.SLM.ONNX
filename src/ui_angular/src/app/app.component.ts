import { Component, ViewChild, ElementRef, NgZone } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  @ViewChild('responseTextArea') responseTextArea!: ElementRef;
  title = 'ONNX Runtime';
  responseData: any = "";
  requestData: any = "What is quantum computing?"; 

  constructor(private http: HttpClient, private ngZone: NgZone) {}

  onGetResponse() {
    this.responseData = "...processing...";
    const observer = {
      next: (response: any) => {
        this.responseData = JSON.stringify(response)
          .replace(/\n/g, ' ')
          .replace(/\r/g, '');
      },
      error: (error: any) => {
        this.responseData = JSON.stringify(error);
      },
      complete: () => {
      }
    };

    const observable: Observable<any> = this.http.get('http://localhost:5225/response?text=' + this.requestData);
    observable.subscribe(observer);
  }

  onGetResponseStream() {
    this.responseData = "";
    const observer = {
      next: (event: any) => {
        console.log(event.type)
        HttpEventType.DownloadProgress
        if (event.type === HttpEventType.DownloadProgress) {
          this.ngZone.run(() => {
            this.responseData = event.partialText
              .replace(/\n/g, ' ')
              .replace(/\r/g, '');
            this.scrollToBottom(); 
          })
        }
        else if (event.type === HttpEventType.Response) {
          // this.ngZone.run(() => {
          //   const partialText = JSON.stringify(event);
          //   this.responseData += event.body; 
          //   console.log('Response from backend:', event.body);
          // });
        }
      },
      error: (error: any) => {
        this.responseData = error.message;
        this.responseData = JSON.stringify(error);
        console.error('Error from backend:', error);
      },
      complete: () => {
      }
    };

    const observable: Observable<any> = this.http.get('http://localhost:5225/responsestream?text=' + this.requestData, {
      responseType: 'text',
      reportProgress: true,
      observe: 'events'
    });
    observable.subscribe(observer);
  }

  private scrollToBottom(): void {
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.responseTextArea.nativeElement.scrollTop = this.responseTextArea.nativeElement.scrollHeight;
      }, 0);
    });
  }

}

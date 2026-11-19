import { CurrencyPipe } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-console-type-list',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './console-type-list.component.html',
  styleUrl: './console-type-list.component.css'
})
export class ConsoleTypeListComponent {

}

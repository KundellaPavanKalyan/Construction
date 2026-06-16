import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-material-tracking-hub',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './material-tracking-hub.component.html',
  styleUrl: './material-tracking-hub.component.scss'
})
export class MaterialTrackingHubComponent {}

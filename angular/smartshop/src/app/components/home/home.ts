import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from '../header/header';
import { Navbar } from '../navbar/navbar';
import { Cart } from '../cart/cart';

@Component({
  selector: 'app-home',
  imports: [RouterOutlet, Header, Navbar, Cart],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {}

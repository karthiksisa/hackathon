import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Contact } from '../models/contact.model';
import { API_BASE_URL } from '../config';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private http = inject(HttpClient);
  private contactsState = signal<Contact[]>([]);

  contacts = this.contactsState.asReadonly();

  constructor() {
    this.loadContacts();
  }

  private loadContacts() {
    this.http.get<Contact[]>(`${API_BASE_URL}/Contacts`).subscribe(contacts => {
      this.contactsState.set(contacts);
    });
  }

  saveContact(contact: Partial<Contact>) {
    if (contact.id) {
      // UpdateContactRequest
      const payload = {
        name: contact.name,
        email: contact.email,
        phone: contact.phone,
        title: contact.title
      };
      this.http.put<void>(`${API_BASE_URL}/Contacts/${contact.id}`, payload).subscribe(() => {
        this.contactsState.update(contacts =>
          contacts.map(c => c.id === contact.id ? { ...c, ...contact } as Contact : c)
        );
      });
    } else {
      // CreateContactRequest
      const payload = {
        accountId: contact.accountId,
        name: contact.name,
        email: contact.email,
        phone: contact.phone,
        title: contact.title
      };
      this.http.post<Contact>(`${API_BASE_URL}/Contacts`, payload).subscribe(newContact => {
        this.contactsState.update(contacts => [...contacts, newContact]);
      });
    }
  }

  deleteContact(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Contacts/${id}`).subscribe(() => {
      this.contactsState.update(contacts => contacts.filter(c => c.id !== id));
    });
  }
}
